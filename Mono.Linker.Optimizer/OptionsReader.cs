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
using System.IO;
using System.Xml;
using System.Xml.XPath;
using Mono.Cecil;

namespace Mono.Linker.Optimizer
{
	using Configuration;

	class OptionsReader
	{
		public OptimizerOptions Options {
			get;
		}

		public string FileName {
			get;
		}

		public static void Read (OptimizerOptions options, string filename)
		{
			var settings = new XmlReaderSettings ();
			var reader = new OptionsReader (options, filename);
			using (var xml = XmlReader.Create (filename, settings)) {
				Console.WriteLine ($"Reading XML description from {filename}.");
				reader.Read (new XPathDocument (xml));
			}
		}

		OptionsReader (OptimizerOptions options, string filename)
		{
			Options = options;
			FileName = filename;
		}

		void Read (XPathDocument document)
		{
			var nav = document.CreateNavigator ();

			var root = nav.SelectSingleNode ("/linker-optimizer");
			if (root == null)
				return;

			var options = root.SelectSingleNode ("options");
			if (options != null)
				OnOptions (options);

			ProcessChildren (root, "include", OnInclude);

			ProcessChildren (root, "features/feature", OnFeature);

			var reader = new ConfigurationReader (Options);
			reader.Read (root);
		}

		void OnInclude (XPathNavigator nav)
		{
			var file = GetAttribute (nav, "filename") ?? throw ThrowError ("<include> requires `filename` argument.");

			if (!Path.IsPathRooted (file)) {
				var directory = Path.GetDirectoryName (Path.GetFullPath (FileName));
				file = Path.Combine (directory, file);
			}

			if (!File.Exists (file))
				throw ThrowError ($"Include file `{file}` does not exist.");
			Read (Options, file);
		}

		void OnOptions (XPathNavigator nav)
		{
			CheckAttribute (nav, "main-debug", value => Options.AutoDebugMain = value);
			CheckAttribute (nav, "all-modules", value => Options.ScanAllModules = value);
			CheckAttribute (nav, "analyze-all", value => Options.AnalyzeAll = value);
			CheckAttribute (nav, "preprocessor", value => Options.SetPreprocessorMode (value));
			CheckAttribute (nav, "no-conditional-redefinition", value => Options.NoConditionalRedefinition = value);
			CheckAttribute (nav, "ignore-resolution-errors", value => Options.IgnoreResolutionErrors = value);
			CheckAttribute (nav, "check-size", value => Options.CheckSize = value);
			CheckAttribute (nav, "report-mode", value => Options.SetReportMode (value));
			CheckAttribute (nav, "report-configuration", value => Options.ReportConfiguration = value);
			CheckAttribute (nav, "report-profile", value => Options.ReportProfile = value);
			CheckAttribute (nav, "size-check-tolerance", value => Options.SizeCheckTolerance = value);
			CheckAttribute (nav, "disable-all", value => Options.DisableAll = value);
		}

		void OnFeature (XPathNavigator nav)
		{
			var name = GetAttribute (nav, "name");
			var value = GetAttribute (nav, "enabled");

			if (value == null || !bool.TryParse (value, out var enabled))
				enabled = true;

			Options.SetFeatureEnabled (name, enabled);
		}

		void CheckAttribute (XPathNavigator nav, string name, Action<bool> action, bool required = false)
		{
			if (GetBoolAttribute (nav, name, out var value))
				action (value);
			else if (required)
				throw ThrowError ($"Missing `{name}` attribute.");
		}

		void CheckAttribute (XPathNavigator nav, string name, Action<string> action, bool required = false)
		{
			var attr = GetAttribute (nav, name);
			if (attr != null)
				action (attr);
			else if (required)
				throw ThrowError ($"Missing `{name}` attribute.");
		}

		bool GetBoolAttribute (XPathNavigator nav, string name, out bool value)
		{
			var attr = GetAttribute (nav, name);
			if (attr != null && bool.TryParse (attr, out value))
				return true;
			value = false;
			return false;
		}

		bool GetName (XPathNavigator nav, out string name, out MatchKind match)
		{
			name = GetAttribute (nav, "name");
			var fullname = GetAttribute (nav, "fullname");
			var substring = GetAttribute (nav, "substring");

			if (fullname != null) {
				match = MatchKind.FullName;
				if (name != null || substring != null)
					return false;
				name = fullname;
			} else if (name != null) {
				match = MatchKind.Name;
				if (fullname != null || substring != null)
					return false;
			} else if (substring != null) {
				match = MatchKind.Substring;
				if (name != null || fullname != null)
					return false;
				name = substring;
			} else {
				match = MatchKind.Name;
				return false;
			}

			return true;
		}

		internal static Exception ThrowError (string message)
		{
			throw new OptimizerException ($"Invalid XML: {message}");
		}

		internal static void ProcessChildren (XPathNavigator nav, string children, Action<XPathNavigator> action)
		{
			var iterator = nav.Select (children);
			while (iterator.MoveNext ())
				action (iterator.Current);
		}

		internal static string GetAttribute (XPathNavigator nav, string attribute)
		{
			var attr = nav.GetAttribute (attribute, string.Empty);
			return string.IsNullOrWhiteSpace (attr) ? null : attr;
		}
	}
}
