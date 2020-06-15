using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.Build.Shared;

namespace Mono.WasmPackager
{
	public class ScanSourceFiles : Microsoft.Build.Utilities.Task
	{
		[Required]
		public string ProjectDirectory {
			get; set;
		}

		[Required]
		public ITaskItem [] SourceFiles {
			get; set;
		}

		[Required]
		// Don't use string here as that wouldn't trim whitespace.
		public ITaskItem Output {
			get; set;
		}

		public override bool Execute ()
		{
			Log.LogMessage (MessageImportance.High, $"WasmPackager - scan source files");

			var ok = true;

			foreach (var file in SourceFiles) {
				bool native = false;
				var isNative = file.GetMetadata ("IsNative");
				if (!string.IsNullOrEmpty (isNative))
					native = bool.Parse (isNative);

				string path = file.ItemSpec, source = null, name;

				if (!Path.IsPathRooted (path))
					path = Path.Combine (ProjectDirectory, path);
				path = Path.GetFullPath (path);

				if (native) {
					source = file.GetMetadata ("Source");
					name = file.GetMetadata ("Name");
				} else {
					name = path;
				}

				var info = new FileInfo (path, native, source, name);

				ok &= ScanFile (info);
			}

			if (!ok)
				return false;

			if (!ResolveAnnotations ())
				return false;

			AutoGenFileHelper.CreateFile (Output.ItemSpec, WriteOutput, Log);

			return true;
		}

		static readonly Regex CommentLine = new Regex ("^\\s*//");
		static readonly Regex CommentRegex = new Regex ("(?://\\s*(.*)\\s*$|/\\*\\s*(.*)\\s*\\*/)");
		static readonly Regex AnnotationRegex = new Regex ("^@@\\s*(\\w+(?:-\\w+)*)\\s*(?::\\s*(\\w+)\\s*)?$");

		List<Annotation> annotations = new List<Annotation> ();
		Dictionary<string, Annotation> annotationByName = new Dictionary<string, Annotation> ();

		bool ScanFile (FileInfo file)
		{
			Log.LogMessage (MessageImportance.Normal, $"  scanning source file: {file.FileName}");

			var ok = true;
			int lineNumber = 0;
			using (var stream = new StreamReader (file.FileName)) {
				string text;
				while ((text = stream.ReadLine ()) != null) {
					++lineNumber;
					foreach (Match match in CommentRegex.Matches (text)) {
						var value = match.Groups [1].Success ? match.Groups [1].Value : match.Groups [2].Value;
						Log.LogMessage (MessageImportance.Low, $"    comment: {match.Index} - |{value}|");

						var annotation = AnnotationRegex.Match (value);
						if (!annotation.Success)
							continue;

						Log.LogMessage (MessageImportance.Low, $"    annotation: |{annotation.Groups [1]}| - |{annotation.Groups [2]}|");

						var line = lineNumber;
						int? column = match.Index;

						// A comment on a line by itself.
						if (CommentLine.Match (text).Success) {
							line++;
							column = null;
						}

						ok &= ProcessAnnotation (file, line, column, annotation.Groups [1].Value, annotation.Groups [2].Value);
					}
				}
			}

			return ok;
		}

		bool ProcessAnnotation (FileInfo file, int line, int? column, string name, string value)
		{
			Log.LogMessage (MessageImportance.Low, $"  annotation: {file.FileName} {line} {column} - {name} {value}");

			bool ExpectArgument ()
			{
				if (string.IsNullOrEmpty (value)) {
					Log.LogError ($"Annotation '{name}' has missing argument in {file}:{line}:{column}.");
					return false;
				}
				return true;
			}

			bool ExpectNoArgument ()
			{
				if (!string.IsNullOrEmpty (value)) {
					Log.LogError ($"Annotation '{name}' has unexpected argument in {file}:{line}:{column}.");
					return false;
				}
				return true;
			}

			bool Add (AnnotationType type, bool expectArgument)
			{
				bool ok;
				if (expectArgument)
					ok = ExpectArgument ();
				else
					ok = ExpectNoArgument ();
				if (!ok)
					return false;
				annotations.Add (new Annotation (type, file, line, column, value));
				return true;
			}

			switch (name) {
			case "BEGIN-FUNCTION":
				return Add (AnnotationType.BeginFunction, true);
			case "END-FUNCTION":
				return Add (AnnotationType.EndFunction, false);
			case "BEGIN-SCOPE":
				return Add (AnnotationType.BeginScope, false);
			case "END-SCOPE":
				return Add (AnnotationType.EndScope, false);
			case "LINE":
				return Add (AnnotationType.Line, true);
			case "BREAKPOINT":
				return Add (AnnotationType.Breakpoint, true);
			default:
				Log.LogError ($"Invalid annotation '{name}' in {file}:{line}:{column}.");
				return false;
			}
		}

		bool ResolveAnnotations ()
		{
			var scopeStack = new Stack<Annotation> ();
			string currentFunction = null;

			var ok = true;

			foreach (var annotation in annotations) {
				switch (annotation.Type) {
				case AnnotationType.BeginFunction:
					if (currentFunction != null) {
						Log.LogError ($"Unexpected annotation: {annotation}.");
						return false;
					}
					currentFunction = annotation.Value;
					break;
				case AnnotationType.EndFunction:
					if (currentFunction == null) {
						Log.LogError ($"Unexpected annotation: {annotation}.");
						return false;
					}
					currentFunction = null;
					break;
				case AnnotationType.BeginScope:
					scopeStack.Push (annotation);
					break;
				case AnnotationType.EndScope:
					scopeStack.Pop ().EndScope = annotation;
					break;
				case AnnotationType.Line:
				case AnnotationType.Breakpoint:
					if (scopeStack.Count > 0)
						annotation.BeginScope = scopeStack.Peek ();
					annotation.Function = currentFunction;
					if (annotationByName.ContainsKey (annotation.Value)) {
						Log.LogError ($"Duplicate annotation: {annotation}.");
						ok = false;
					}
					annotationByName.Add (annotation.Value, annotation);
					break;
				default:
					throw new InvalidOperationException ();
				}
			}

			if (scopeStack.Count > 0) {
				Log.LogError ($"Unbalanced being/end scope annotations.");
				return false;
			}

			foreach (var annotation in annotationByName.Values) {
				if (annotation.BeginScope != null) {
					annotation.EndScope = annotation.BeginScope.EndScope;
					if (annotation.EndScope == null) {
						Log.LogError ($"Unbalanced begin/end scopes in annotation: {annotation}");
						ok = false;
					}
				}
			}

			return ok;
		}

		void WriteOutput (TextWriter writer)
		{
			writer.WriteLine ("using Mono.WasmPackager.TestSuite;");
			writer.WriteLine ();
			writer.WriteLine ("namespace Mono.WasmPackager.DevServer");
			writer.WriteLine ("{");
			writer.WriteLine ("\tpartial class TestSettings");
			writer.WriteLine ("\t{");
			writer.WriteLine ("\t\tpublic static class Locations");
			writer.WriteLine ("\t\t{");
			foreach (var annotation in annotationByName.Values) {
				var outputLine = annotation.CreateOutputLine ();
				writer.WriteLine ($"\t\t\t{outputLine}");
				Log.LogMessage (MessageImportance.Normal, $"  adding annotation: {annotation}");
			}
			writer.WriteLine ("\t\t}");
			writer.WriteLine ("\t}");
			writer.WriteLine ("}");
		}

		public enum AnnotationType
		{
			BeginFunction,
			EndFunction,
			BeginScope,
			EndScope,
			Line,
			Breakpoint
		}

		public class Annotation
		{
			public AnnotationType Type {
				get;
			}

			public FileInfo File {
				get;
			}

			public int Line {
				get;
			}

			public int? Column {
				get;
			}

			public string Function {
				get; set;
			}

			public string Value {
				get;
			}

			public Annotation BeginScope {
				get; set;
			}

			public Annotation EndScope {
				get; set;
			}

			public Annotation (AnnotationType type, FileInfo file, int line, int? column, string value)
			{
				Type = type;
				File = file;
				Line = line;
				Column = column;
				Value = value;
			}

			public string CreateOutputLine ()
			{
				var args = new List<string> ();
				args.Add ($"\"{File.FileName}\"");
				args.Add ($"\"{File.Name}\"");
				args.Add (File.Source != null ? $"\"{File.Source}\"" : "null");
				args.Add (File.IsNative ? "true" : "false");
				var function = !string.IsNullOrEmpty (Function) ? Function : Value;
				args.Add ($"\"{function}\"");
				args.Add (Line.ToString ());
				args.Add (false && Column != null ? Column.ToString () : "null");

				if (BeginScope != null) {
					args.Add (BeginScope.Line.ToString ());
					args.Add (EndScope.Line.ToString ());
				}

				var ctorArgs = string.Join (", ", args);

				return $"public static readonly SourceLocation {Value} = new SourceLocation ({ctorArgs});";
			}

			public override string ToString () => $"[{Type} {File}:{Line}:{Column ?? 0}{(!string.IsNullOrEmpty (Value) ? " " : "")}{Value}]";
		}
	}

	public class FileInfo
	{
		public string FileName {
			get;
		}

		public bool IsNative {
			get;
		}

		public string Source {
			get;
		}

		public string Name {
			get;
		}

		public FileInfo (string file, bool isNative, string source, string name)
		{
			FileName = file;
			IsNative = isNative;
			Source = source;
			Name = name;
		}
	}
}
