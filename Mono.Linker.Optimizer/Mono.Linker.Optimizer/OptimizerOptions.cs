//
// OptimizerOptions.cs
//
// Author:
//       Martin Baulig <mabaul@microsoft.com>
//
// Copyright (c) 2019 Microsoft Corporation
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Mono.Cecil;

namespace Mono.Linker.Optimizer
{
	using Configuration;

	public class OptimizerOptions
	{
		public bool ScanAllModules {
			get; set;
		}

		public bool AnalyzeAll {
			get; set;
		}

		public bool DeadCodeElimination {
			get; set;
		}

		public PreprocessorMode Preprocessor {
			get; set;
		}

		public bool NoConditionalRedefinition {
			get; set;
		}

		public bool IgnoreResolutionErrors {
			get; set;
		}

		public bool CheckSize {
			get; set;
		}

		public string ReportConfiguration {
			get; set;
		}

		public string ReportProfile {
			get; set;
		}

		public string SizeCheckTolerance {
			get; set;
		}

		public bool AutoDebugMain {
			get; set;
		}

		public bool DisableModule {
			get; set;
		}

		public string ReportFileName {
			get; set;
		}

		public string CompareWith {
			get; set;
		}

		public bool DisableAll {
			get; set;
		}

		public bool EnableBlazor {
			get; set;
		}

		public ReportMode ReportMode {
			get; set;
		}

		public OptimizerConfiguration OptimizerConfiguration {
			get;
		}

		public OptimizerReport OptimizerReport {
			get;
		}

		public List<string> AssemblyReferences {
			get;
		}

		readonly Dictionary<MonoLinkerFeature, bool> _enabled_features;

		public OptimizerOptions ()
		{
			NoConditionalRedefinition = true;
			DeadCodeElimination = true;
			AssemblyReferences = new List<string> ();
			_enabled_features = new Dictionary<MonoLinkerFeature, bool> {
				[MonoLinkerFeature.Unknown] = false,
				[MonoLinkerFeature.Martin] = false
			};

			OptimizerConfiguration = new OptimizerConfiguration ();
			OptimizerReport = new OptimizerReport (this);
		}

		internal void ParseOptions (string options)
		{
			var parts = options.Split (',');
			for (int i = 0; i < parts.Length; i++) {
				var part = parts [i];

				var pos = part.IndexOf ('=');
				if (pos > 0) {
					var name = part.Substring (0, pos);
					var value = part.Substring (pos + 1);
					if (string.IsNullOrWhiteSpace (value))
						value = null;

					switch (name) {
					case "preprocess":
						SetPreprocessorMode (value);
						continue;
					case "report-configuration":
						ReportConfiguration = value;
						continue;
					case "report-profile":
						ReportProfile = value;
						continue;
					case "size-check-tolerance":
						SizeCheckTolerance = value;
						continue;
					case "report-mode":
						SetReportMode (value);
						continue;
					case "compare-with":
						CompareWith = value;
						continue;
					default:
						throw new OptimizerException ($"Unknown option `{part}`.");
					}
				}

				bool? enabled = null;
				if (part [0] == '+') {
					part = part.Substring (1);
					enabled = true;
				} else if (part [0] == '-') {
					part = part.Substring (1);
					enabled = false;
				}

				switch (part) {
				case "main-debug":
					AutoDebugMain = enabled ?? true;
					break;
				case "all-modules":
					ScanAllModules = enabled ?? true;
					break;
				case "analyze-all":
					AnalyzeAll = enabled ?? true;
					break;
				case "dead-code-elimination":
					DeadCodeElimination = enabled ?? true;
					break;
				case "no-conditional-redefinition":
					NoConditionalRedefinition = enabled ?? true;
					break;
				case "ignore-resolution-errors":
					IgnoreResolutionErrors = enabled ?? true;
					break;
				case "check-size":
					CheckSize = enabled ?? true;
					continue;
				case "disable-module":
					DisableModule = enabled ?? true;
					break;
				case "disable-all":
					DisableAll = enabled ?? true;
					break;
				case "blazor":
					EnableBlazor = enabled ?? true;
					break;
				default:
					SetFeatureEnabled (part, enabled ?? false);
					break;
				}
			}
		}

		public void SetPreprocessorMode (string argument)
		{
			if (!Enum.TryParse (argument, true, out PreprocessorMode mode))
				throw new OptimizerException ($"Invalid preprocessor mode: `{argument}`.");
			Preprocessor = mode;
		}

		public void SetReportMode (string argument)
		{
			var parts = argument.Split ('+');
			for (int i = 0; i < parts.Length; i++) {
				switch (parts[i].ToLowerInvariant ()) {
				case "dead-code":
					ReportMode |= ReportMode.DeadCode;
					continue;
				case "detailed-methods":
					ReportMode |= ReportMode.Detailed | ReportMode.DetailedMethods;
					continue;
				}
				if (!Enum.TryParse (parts[i], true, out ReportMode mode))
					throw new OptimizerException ($"Invalid report mode: `{argument}`.");
				ReportMode |= mode;
			}
		}

		public bool IsFeatureEnabled (string feature) => IsFeatureEnabled (FeatureByName (feature));

		public bool IsFeatureEnabled (MonoLinkerFeature feature)
		{
			if (_enabled_features.TryGetValue (feature, out var value))
				return value;
			return !DisableAll;
		}

		public void SetFeatureEnabled (MonoLinkerFeature feature, bool enabled)
		{
			if (feature == MonoLinkerFeature.Unknown)
				throw new OptimizerException ($"Cannot modify `{nameof (MonoLinkerFeature.Unknown)}`.");
			_enabled_features [feature] = enabled;
		}

		public void SetFeatureEnabled (string name, bool enabled)
		{
			var feature = FeatureByName (name);
			_enabled_features [feature] = enabled;
		}

		internal static MonoLinkerFeature FeatureByName (string name)
		{
			switch (name.ToLowerInvariant ()) {
			case "sre":
			case "reflection-emit":
				return MonoLinkerFeature.ReflectionEmit;
			case "martin":
				return MonoLinkerFeature.Martin;
			default:
				if (!Enum.TryParse<MonoLinkerFeature> (name, true, out var feature))
					throw new OptimizerException ($"Unknown linker feature `{name}`.");
				return feature;
			}
		}

		internal static bool TryParseTypeAction (string name, out TypeAction action)
		{
			return Enum.TryParse (name, true, out action);
		}

		internal static bool TryParseMethodAction (string name, out MethodAction action)
		{
			switch (name.ToLowerInvariant ()) {
			case "return-null":
				action = MethodAction.ReturnNull;
				return true;
			case "return-false":
				action = MethodAction.ReturnFalse;
				return true;
			case "return-true":
				action = MethodAction.ReturnTrue;
				return true;
			default:
				return Enum.TryParse (name, true, out action);
			}
		}

		bool DontDebugThis (TypeDefinition type)
		{
			if (type.DeclaringType != null)
				return DontDebugThis (type.DeclaringType);

			switch (type.FullName) {
			case "Martin.LinkerTest.TestHelpers":
			case "Martin.LinkerTest.AssertionException":
				return true;
			default:
				return false;
			}
		}

		public bool EnableDebugging (OptimizerContext context, TypeDefinition type)
		{
			if (type.DeclaringType != null)
				return EnableDebugging (context, type.DeclaringType);

			if (DontDebugThis (type))
				return false;

			if (AutoDebugMain) {
				if (type.Namespace == "Martin.LinkerTest")
					return true;
				if (type.Module.Assembly.Name.Name.ToLowerInvariant ().Contains ("martin"))
					return true;
			}

			return context.GetTypeEntries (type)?.Any (t => t.Action == TypeAction.Debug) ?? false;
		}

		public bool EnableDebugging (OptimizerContext context, MethodDefinition method)
		{
			if (DontDebugThis (method.DeclaringType))
				return false;
			if (EnableDebugging (context, method.DeclaringType))
				return true;

			if (AutoDebugMain) {
				if (method.Name == "Main")
					return true;
				if (method.FullName.Contains ("Martin"))
					return true;
			}

			return context.GetMethodEntries (method)?.Any (m => m.Action == MethodAction.Debug) ?? false;
		}

		public void CheckFailList (OptimizerContext context, TypeDefinition type, string original = null)
		{
			var entry = context.GetTypeEntries (type)?.FirstOrDefault (t => t.Action == TypeAction.Warn || t.Action == TypeAction.Fail);
			if (entry == null)
				return;

			var original_message = original != null ? $" while parsing `{original}`" : string.Empty;
			var message = $"Found fail-listed type `{type.FullName}`";
			context.LogMessage (MessageImportance.High, Environment.NewLine);
			context.LogMessage (MessageImportance.High, message + ":");
			DumpFailEntry (context, entry);
			var stack = context.DumpTracerStack ();
			OptimizerReport.FailList.Add (new FailListEntry (type, entry, original, stack));
			context.LogMessage (MessageImportance.High, Environment.NewLine);
			if (entry.Action == TypeAction.Fail)
				throw new OptimizerException (message + original_message + ".");
		}

		public void CheckFailList (OptimizerContext context, MethodDefinition method)
		{
			CheckFailList (context, method.DeclaringType, method.FullName);

			var entry = context.GetMethodEntries (method)?.FirstOrDefault (m => m.Action == MethodAction.Warn || m.Action == MethodAction.Fail);
			if (entry == null)
				return;

			var message = $"Found fail-listed method `{method.FullName}`";
			context.LogMessage (MessageImportance.High, Environment.NewLine);
			context.LogMessage (MessageImportance.High, message + ":");
			DumpFailEntry (context, entry);
			var stack = context.DumpTracerStack ();
			OptimizerReport.FailList.Add (new FailListEntry (method, entry, stack));
			context.LogMessage (MessageImportance.High, Environment.NewLine);
			if (entry.Action == MethodAction.Fail)
				throw new OptimizerException (message + ".");
		}

		static void DumpFailEntry (OptimizerContext context, Type type)
		{
			context.LogMessage (MessageImportance.High, "  " + type);
			if (type.Parent != null)
				DumpFailEntry (context, type.Parent);
		}

		static void DumpFailEntry (OptimizerContext context, Method method)
		{
			context.LogMessage (MessageImportance.High, "  " + method);
			if (method.Parent != null)
				DumpFailEntry (context, method.Parent);
		}

		public enum PreprocessorMode
		{
			None,
			Disabled,
			Automatic,
			Full
		}
	}
}
