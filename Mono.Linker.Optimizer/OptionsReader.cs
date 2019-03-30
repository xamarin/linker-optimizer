//
// OptionsReader.cs
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
using System.Xml.XPath;
using Mono.Cecil;

namespace Mono.Linker.Optimizer
{
	class OptionsReader
	{
		public OptimizerOptions Options {
			get;
		}

		public static void Read (OptimizerOptions options, XPathDocument document)
		{
			var reader = new OptionsReader (options);
			reader.Read (document);
		}

		public static void Read (OptimizerOptions options, string filename)
		{
			var reader = new OptionsReader (options);
			reader.Read (new XPathDocument (filename));
		}

		OptionsReader (OptimizerOptions options)
		{
			Options = options;
		}

		void Read (XPathDocument document)
		{
			var nav = document.CreateNavigator ();

			var root = nav.SelectSingleNode ("/linker/martin");
			if (root == null)
				return;

			var node = root.SelectSingleNode ("options");
			if (node != null)
				OnOptions (node);

			ProcessChildren (root, "features/feature", OnFeature);
			ProcessChildren (root, "conditional", OnConditional);

			ProcessChildren (root, "namespace", child => OnNamespaceEntry (child));
			ProcessChildren (root, "type", child => OnTypeEntry (child, null));
			ProcessChildren (root, "method", child => OnMethodEntry (child));
		}

		void OnOptions (XPathNavigator nav)
		{
			if (GetBoolAttribute (nav, "main-debug", out var value))
				Options.AutoDebugMain = value;

			if (GetBoolAttribute (nav, "all-modules", out value))
				Options.ScanAllModules = value;

			if (GetBoolAttribute (nav, "analyze-all", out value))
				Options.AnalyzeAll = value;

			if (GetBoolAttribute (nav, "preprocess", out value))
				Options.Preprocess = value;

			if (GetBoolAttribute (nav, "no-conditional-redefinition", out value))
				Options.NoConditionalRedefinition = value;

			if (GetBoolAttribute (nav, "ignore-resolution-errors", out value))
				Options.IgnoreResolutionErrors = value;

			if (GetBoolAttribute (nav, "report-size", out value))
				Options.ReportSize = value;
		}

		void OnFeature (XPathNavigator nav)
		{
			var name = GetAttribute (nav, "name");
			var value = GetAttribute (nav, "enabled");

			if (string.IsNullOrEmpty (value) || !bool.TryParse (value, out var enabled))
				enabled = true;

			Options.SetFeatureEnabled (name, enabled);
		}

		void OnConditional (XPathNavigator nav)
		{
			var name = GetAttribute (nav, "feature");
			if (string.IsNullOrEmpty (name) || !GetBoolAttribute (nav, "enabled", out var enabled))
				throw ThrowError ("<conditional> needs both `feature` and `enabled` arguments.");

			var feature = OptimizerOptions.FeatureByName (name);

			ProcessChildren (nav, "namespace", child => OnNamespaceEntry (child, Conditional));
			ProcessChildren (nav, "type", child => OnTypeEntry (child, null, Conditional));
			ProcessChildren (nav, "method", child => OnMethodEntry (child, null, Conditional));

			bool Conditional (MemberReference reference) => Options.IsFeatureEnabled (feature) == enabled;
		}

		bool GetBoolAttribute (XPathNavigator nav, string name, out bool value)
		{
			var attr = GetAttribute (nav, name);
			if (!string.IsNullOrEmpty (attr) && bool.TryParse (attr, out value))
				return true;
			value = false;
			return false;
		}

		bool GetName (XPathNavigator nav, out string name, out OptimizerOptions.MatchKind match)
		{
			name = GetAttribute (nav, "name");
			var fullname = GetAttribute (nav, "fullname");
			var substring = GetAttribute (nav, "substring");

			if (!string.IsNullOrEmpty (fullname)) {
				match = OptimizerOptions.MatchKind.FullName;
				if (!string.IsNullOrEmpty (name) || !string.IsNullOrEmpty (substring))
					return false;
				name = fullname;
			} else if (!string.IsNullOrEmpty (name)) {
				match = OptimizerOptions.MatchKind.Name;
				if (!string.IsNullOrEmpty (fullname) || !string.IsNullOrEmpty (substring))
					return false;
			} else if (!string.IsNullOrEmpty (substring)) {
				match = OptimizerOptions.MatchKind.Substring;
				if (!string.IsNullOrEmpty (name) || !string.IsNullOrEmpty (fullname))
					return false;
				name = substring;
			} else {
				match = OptimizerOptions.MatchKind.Name;
				return false;
			}

			return true;
		}

		void OnNamespaceEntry (XPathNavigator nav, Func<MemberReference, bool> conditional = null)
		{
			var name = GetAttribute (nav, "name");
			if (string.IsNullOrEmpty (name))
				throw ThrowError ("<namespace> entry needs `name` attribute.");

			OptimizerOptions.TypeEntry entry;
			var action = GetAttribute (nav, "action");
			if (!string.IsNullOrEmpty (action))
				entry = AddTypeEntry (name, OptimizerOptions.MatchKind.Namespace, action, null, conditional);
			else
				entry = Options.AddTypeEntry (name, OptimizerOptions.MatchKind.Namespace, OptimizerOptions.TypeAction.None, null, conditional);

			ProcessChildren (nav, "type", child => OnTypeEntry (child, entry, conditional));
			ProcessChildren (nav, "method", child => OnMethodEntry (child, entry, conditional));
		}

		void OnTypeEntry (XPathNavigator nav, OptimizerOptions.TypeEntry parent = null, Func<MemberReference, bool> conditional = null)
		{
			if (!GetName (nav, out var name, out var match))
				throw ThrowError ($"Ambiguous name in type entry `{nav.OuterXml}`.");

			OptimizerOptions.TypeEntry entry;
			var action = GetAttribute (nav, "action");
			if (!string.IsNullOrEmpty (action))
				entry = AddTypeEntry (name, match, action, parent, conditional);
			else
				entry = Options.AddTypeEntry (name, match, OptimizerOptions.TypeAction.None, parent, conditional);

			ProcessChildren (nav, "method", child => OnMethodEntry (child, entry, conditional));
		}

		OptimizerOptions.TypeEntry AddTypeEntry (string name, OptimizerOptions.MatchKind match, string action, OptimizerOptions.TypeEntry parent = null, Func<MemberReference, bool> conditional = null)
		{
			if (!OptimizerOptions.TryParseTypeAction (action, out var typeAction))
				throw ThrowError ($"Invalid `action` attribute: `{action}`.");

			return Options.AddTypeEntry (name, match, typeAction, parent, conditional);
		}

		void OnMethodEntry (XPathNavigator nav, OptimizerOptions.TypeEntry parent = null, Func<MemberReference, bool> conditional = null)
		{
			if (!GetName (nav, out var name, out var match))
				throw ThrowError ($"Ambiguous name in method entry `{nav.OuterXml}`.");

			var action = GetAttribute (nav, "action");
			if (string.IsNullOrEmpty (action))
				throw ThrowError ($"Missing `action` attribute in {nav.OuterXml}.");

			if (!OptimizerOptions.TryParseMethodAction (action, out var methodAction))
				throw ThrowError ($"Invalid `action` attribute in {nav.OuterXml}.");

			Options.AddMethodEntry (name, match, methodAction, parent, conditional);
		}

		Exception ThrowError (string message)
		{
			throw new NotSupportedException ($"Invalid XML: {message}");
		}

		static void ProcessChildren (XPathNavigator nav, string children, Action<XPathNavigator> action)
		{
			var iterator = nav.Select (children);
			while (iterator.MoveNext ())
				action (iterator.Current);
		}

		static string GetAttribute (XPathNavigator nav, string attribute)
		{
			return nav.GetAttribute (attribute, string.Empty);
		}
	}
}
