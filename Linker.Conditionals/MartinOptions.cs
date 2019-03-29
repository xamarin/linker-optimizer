//
// MartinOptions.cs
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

namespace Mono.Linker.Conditionals
{
	public class MartinOptions
	{
		public bool ScanAllModules {
			get; set;
		}

		public bool AnalyzeAll {
			get; set;
		}

		public bool Preprocess {
			get; set;
		}

		public bool NoConditionalRedefinition {
			get; set;
		}

		public bool IgnoreResolutionErrors {
			get; set;
		}

		public bool ReportSize {
			get; set;
		}

		public bool AutoDebugMain {
			get; set;
		}

		readonly List<TypeEntry> _type_actions;
		readonly List<MethodEntry> _method_actions;
		readonly Dictionary<MonoLinkerFeature, bool> _enabled_features;

		public MartinOptions ()
		{
			AutoDebugMain = true;
			NoConditionalRedefinition = true;
			_type_actions = new List<TypeEntry> ();
			_method_actions = new List<MethodEntry> ();
			_enabled_features = new Dictionary<MonoLinkerFeature, bool> {
				[MonoLinkerFeature.Unknown] = false,
				[MonoLinkerFeature.Martin] = false
			};
		}

		internal void CheckEnvironmentOptions ()
		{
			var options = Environment.GetEnvironmentVariable ("MARTIN_LINKER_OPTIONS");
			if (string.IsNullOrEmpty (options))
				return;

			var parts = options.Split (',');
			for (int i = 0; i < parts.Length; i++) {
				var part = parts [i];
				bool? enabled = null;
				if (part [0] == '+') {
					part = part.Substring (1);
					enabled = true;
				} else if (part [1] == '-') {
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
				case "preprocess":
					Preprocess = enabled ?? true;
					break;
				case "no-conditional-redefinition":
					NoConditionalRedefinition = enabled ?? true;
					break;
				case "ignore-resolution-errors":
					IgnoreResolutionErrors = enabled ?? true;
					break;
				case "report-size":
					ReportSize = enabled ?? true;
					break;
				default:
					SetFeatureEnabled (part, enabled ?? false);
					break;
				}
			}
		}

		public bool IsFeatureEnabled (MonoLinkerFeature feature)
		{
			if (_enabled_features.TryGetValue (feature, out var value))
				return value;
			return true;
		}

		public void SetFeatureEnabled (MonoLinkerFeature feature, bool enabled)
		{
			if (feature == MonoLinkerFeature.Unknown)
				throw new NotSupportedException ($"Cannot modify `{nameof (MonoLinkerFeature.Unknown)}`.");
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
				return MonoLinkerFeature.ReflectionEmit;
			case "martin":
				return MonoLinkerFeature.Martin;
			default:
				if (!Enum.TryParse<MonoLinkerFeature> (name, true, out var feature))
					throw new NotSupportedException ($"Unknown linker feature `{name}`.");
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

		public bool EnableDebugging (TypeDefinition type)
		{
			if (type.DeclaringType != null)
				return EnableDebugging (type.DeclaringType);

			if (DontDebugThis (type))
				return false;

			if (AutoDebugMain) {
				if (type.Namespace == "Martin.LinkerTest")
					return true;
				if (type.Module.Assembly.Name.Name.ToLowerInvariant ().Contains ("martin"))
					return true;
			}

			return _type_actions.Any (t => t.Matches (type, TypeAction.Debug));
		}

		public bool EnableDebugging (MethodDefinition method)
		{
			if (DontDebugThis (method.DeclaringType))
				return false;
			if (EnableDebugging (method.DeclaringType))
				return true;

			if (AutoDebugMain) {
				if (method.Name == "Main")
					return true;
				if (method.FullName.Contains ("Martin"))
					return true;
			}

			return _method_actions.Any (e => e.Matches (method, MethodAction.Debug));
		}

		public bool FailOnMethod (MethodDefinition method)
		{
			if (HasTypeEntry (method.DeclaringType, TypeAction.Fail))
				return true;

			return _method_actions.Any (e => e.Matches (method, MethodAction.Fail));
		}

		public void CheckFailList (MartinContext context, TypeDefinition type, string original = null)
		{
			if (type.DeclaringType != null) {
				CheckFailList (context, type.DeclaringType, original ?? type.FullName);
				return;
			}

			var fail = _type_actions.FirstOrDefault (e => e.Matches (type, TypeAction.Fail));
			var warn = _type_actions.FirstOrDefault (e => e.Matches (type, TypeAction.Warn));
			if (fail == null && warn == null)
				return;

			var original_message = original != null ? $" while parsing `{original}`" : string.Empty;
			var message = $"Found fail-listed type `{type.FullName}`";
			context.LogMessage (MessageImportance.High, Environment.NewLine);
			context.LogMessage (MessageImportance.High, message + ":");
			DumpFailEntry (context, fail ?? warn);
			context.Context.Tracer.Dump ();
			context.LogMessage (MessageImportance.High, Environment.NewLine);
			if (fail != null)
				throw new NotSupportedException (message + original_message + ".");
		}

		public void CheckFailList (MartinContext context, MethodDefinition method)
		{
			CheckFailList (context, method.DeclaringType, method.FullName);

			var fail = _method_actions.FirstOrDefault (e => e.Matches (method, MethodAction.Fail));
			var warn = _method_actions.FirstOrDefault (e => e.Matches (method, MethodAction.Warn));
			if (fail == null && warn == null)
				return;

			var message = $"Found fail-listed method `{method.FullName}`";
			context.LogMessage (MessageImportance.High, Environment.NewLine);
			context.LogMessage (MessageImportance.High, message + ":");
			DumpFailEntry (context, fail ?? warn);
			context.Context.Tracer.Dump ();
			context.LogMessage (MessageImportance.High, Environment.NewLine);
			if (fail != null)
				throw new NotSupportedException (message + ".");
		}

		static void DumpFailEntry (MartinContext context, TypeEntry entry)
		{
			context.LogMessage (MessageImportance.High, "  " + entry.ToString ());
			if (entry.Parent != null)
				DumpFailEntry (context, entry.Parent);
		}

		static void DumpFailEntry (MartinContext context, MethodEntry entry)
		{
			context.LogMessage (MessageImportance.High, "  " + entry.ToString ());
			if (entry.Parent != null)
				DumpFailEntry (context, entry.Parent);
		}

		public TypeEntry AddTypeEntry (string name, MatchKind match, TypeAction action, TypeEntry parent, Func<MemberReference, bool> conditional)
		{
			var entry = new TypeEntry (name, match, action, parent, conditional);
			_type_actions.Add (entry);
			return entry;
		}

		public void AddMethodEntry (string name, MatchKind match, MethodAction action, TypeEntry parent = null, Func<MemberReference, bool> conditional = null)
		{
			_method_actions.Add (new MethodEntry (name, match, action, parent, conditional));
		}

		public bool HasTypeEntry (TypeDefinition type, TypeAction action)
		{
			if (type.DeclaringType != null)
				return HasTypeEntry (type.DeclaringType, action);
			return _type_actions.Any (e => e.Matches (type, action));
		}

		public void ProcessTypeEntries (TypeDefinition type, Action<TypeAction> action)
		{
			if (type.DeclaringType != null) {
				ProcessTypeEntries (type.DeclaringType, action);
				return;
			}
			foreach (var entry in _type_actions) {
				if (entry.Action != TypeAction.None && entry.Matches (type))
					action (entry.Action);
			}
		}

		public void ProcessTypeEntries (TypeDefinition type, TypeAction filter, Action action)
		{
			if (type.DeclaringType != null) {
				ProcessTypeEntries (type.DeclaringType, filter, action);
				return;
			}
			foreach (var entry in _type_actions) {
				if (entry.Action == filter && entry.Matches (type))
					action ();
			}
		}

		public void ProcessMethodEntries (MethodDefinition method, Action<MethodAction> action)
		{
			foreach (var entry in _method_actions) {
				if (entry.Action != MethodAction.None && entry.Matches (method))
					action (entry.Action);
			}
		}

		public void ProcessMethodEntries (MethodDefinition method, MethodAction filter, Action action)
		{
			foreach (var entry in _method_actions) {
				if (entry.Action == filter && entry.Matches (method))
					action ();
			}
		}

		public enum TypeAction
		{
			None,
			Debug,
			Fail,
			Warn,
			Size,
			Preserve
		}

		public enum MethodAction
		{
			None,
			Debug,
			Fail,
			Warn,
			Throw,
			ReturnFalse,
			ReturnNull
		}

		public enum MatchKind
		{
			Name,
			FullName,
			Substring,
			Namespace
		}

		public class TypeEntry
		{
			public string Name {
				get;
			}

			public MatchKind Match {
				get;
			}

			public TypeAction Action {
				get;
			}

			public TypeEntry Parent {
				get;
			}

			public Func<TypeDefinition, bool> Conditional {
				get;
			}

			public bool Matches (TypeDefinition type, TypeAction? action = null)
			{
				if (action != null && action.Value != Action)
					return false;
				if (Conditional != null && !Conditional (type))
					return false;
				if (Parent != null && !Parent.Matches (type))
					return false;

				switch (Match) {
				case MatchKind.FullName:
					return type.FullName == Name;
				case MatchKind.Substring:
					return type.FullName.Contains (Name);
				case MatchKind.Namespace:
					return type.Namespace.StartsWith (Name, StringComparison.InvariantCulture);
				default:
					return type.Name == Name;
				}
			}

			public TypeEntry (string name, MatchKind match, TypeAction action, TypeEntry parent, Func<MemberReference, bool> conditional)
			{
				Name = name;
				Match = match;
				Action = action;
				Parent = parent;
				Conditional = conditional;
			}

			public override string ToString ()
			{
				return $"[{GetType ().Name} {Name} {Match} {Action}]";
			}
		}

		public class MethodEntry
		{
			public string Name {
				get;
			}

			public MatchKind Match {
				get;
			}

			public MethodAction Action {
				get;
			}

			public TypeEntry Parent {
				get;
			}

			public Func<MethodDefinition, bool> Conditional {
				get;
			}

			public bool Matches (MethodDefinition method, MethodAction? action = null)
			{
				if (action != null && action.Value != Action)
					return false;
				if (Conditional != null && !Conditional (method))
					return false;
				if (Parent != null && !Parent.Matches (method.DeclaringType))
					return false;

				switch (Match) {
				case MatchKind.FullName:
					return method.FullName == Name;
				case MatchKind.Substring:
					return method.FullName.Contains (Name);
				default:
					return method.Name == Name;
				}
			}

			public bool Matches (MethodDefinition method, MethodAction action) => Action == action && Matches (method);

			public MethodEntry (string name, MatchKind match, MethodAction action, TypeEntry parent = null, Func<MemberReference, bool> conditional = null)
			{
				Name = name;
				Match = match;
				Action = action;
				Parent = parent;
				Conditional = conditional;
			}

			public override string ToString ()
			{
				return $"[{GetType ().Name} {Name}:{Match}:{Action}]";
			}
		}
	}
}
