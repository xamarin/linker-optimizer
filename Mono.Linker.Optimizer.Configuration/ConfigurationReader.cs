//
// ConfigurationReader.cs
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

namespace Mono.Linker.Optimizer.Configuration
{
	public class ConfigurationReader
	{
		public OptimizerConfiguration Root {
			get;
		}

		public bool NeedPreprocessor {
			get;
			private set;
		}

		public ConfigurationReader (OptimizerConfiguration root)
		{
			Root = root;
		}

		public void Read (XPathNavigator nav)
		{
			nav.ProcessChildren ("conditional", Root.ActionList.Children, OnConditional);

			nav.ProcessChildren ("namespace", Root.ActionList.Children, OnNamespaceEntry);
			nav.ProcessChildren ("type", (Type)null, Root.ActionList.Children, OnTypeEntry);
			nav.ProcessChildren ("method", (Type)null, Root.ActionList.Children, OnMethodEntry);

			nav.ProcessChildren ("size-check", child => OnSizeCheckEntry (child, Root.SizeCheck));

			nav.ProcessChildren ("size-report", child => OnSizeReportEntry (child, Root.SizeReport));
		}

		ActionList OnConditional (XPathNavigator nav)
		{
			var name = nav.GetAttribute ("feature");
			if (name == null || !nav.GetBoolAttribute ("enabled", out var enabled))
				throw ThrowError ("<conditional> needs both `feature` and `enabled` arguments.");

			OptimizerOptions.FeatureByName (name);

			var conditional = new ActionList (name, enabled);

			nav.ProcessChildren ("namespace", conditional.Children, OnNamespaceEntry);
			nav.ProcessChildren ("type", (Type)null, conditional.Children, OnTypeEntry);
			nav.ProcessChildren ("method", (Type)null, conditional.Children, OnMethodEntry);

			return conditional;
		}

		Type OnNamespaceEntry (XPathNavigator nav)
		{
			var name = nav.GetAttribute ("name") ?? throw ThrowError ("<namespace> entry needs `name` attribute.");

			var action = nav.GetTypeAction ("action");
			var node = new Type (null, name, null, MatchKind.Namespace, action);

			nav.ProcessChildren ("type", node, node.Types, OnTypeEntry);
			nav.ProcessChildren ("method", node, node.Methods, OnMethodEntry);

			return node;
		}

		Type OnTypeEntry (XPathNavigator nav, Type parent)
		{
			if (!GetName (nav, out var name, out var match))
				throw ThrowError ($"Ambiguous name in type entry `{nav.OuterXml}`.");

			var action = nav.GetTypeAction ("action");
			var type = new Type (parent, name, null, match, action);

			nav.ProcessChildren ("type", parent, type.Types, OnTypeEntry);
			nav.ProcessChildren ("method", parent, type.Methods, OnMethodEntry);

			return type;
		}

		Method OnMethodEntry (XPathNavigator nav, Type parent)
		{
			if (!GetName (nav, out var name, out var match))
				throw ThrowError ($"Ambiguous name in method entry `{nav.OuterXml}`.");

			MethodAction? action = null;
			var attribute = nav.GetAttribute ("action");
			if (attribute != null && !nav.TryGetMethodAction ("action", out action))
				throw ThrowError ($"Cannot parse `action` attribute in {nav.OuterXml}.");

			switch (action ?? MethodAction.None) {
			case MethodAction.ReturnFalse:
			case MethodAction.ReturnTrue:
			case MethodAction.ReturnNull:
			case MethodAction.Throw:
				NeedPreprocessor = true;
				break;
			}

			return new Method (parent, name, match, action);
		}

		void OnSizeCheckEntry (XPathNavigator nav, SizeCheck parent)
		{
			nav.ProcessChildren ("configuration", parent, parent.Configurations, OnConfigurationEntry);
		}

		Configuration OnConfigurationEntry (XPathNavigator nav, SizeCheck parent)
		{
			var name = nav.GetAttribute ("name");
			var configuration = Root.SizeCheck.Configurations.GetChild (c => c.Name == name, () => new Configuration (name));
			nav.ProcessChildren ("profile", configuration, configuration.Profiles, OnProfileEntry);
			return configuration;
		}

		Profile OnProfileEntry (XPathNavigator nav, Configuration configuration)
		{
			var name = nav.GetAttribute ("name");
			var profile = configuration.Profiles.GetChild (p => p.Name == name, () => new Profile (name));
			nav.ProcessChildren ("assembly", profile.Assemblies, OnAssembly);
			return profile;
		}

		void OnSizeReportEntry (XPathNavigator nav, SizeReport parent)
		{
			nav.ProcessChildren ("assembly", parent.Assemblies, OnAssembly);
		}

		Assembly OnAssembly (XPathNavigator nav)
		{
			var name = OptionsReader.GetAttribute (nav, "name") ?? throw OptionsReader.ThrowError ("<assembly> requires `name` attribute.");
			var sizeAttr = OptionsReader.GetAttribute (nav, "size");
			if (sizeAttr == null || !int.TryParse (sizeAttr, out var size))
				throw OptionsReader.ThrowError ("<assembly> requires `size` attribute.");
			var tolerance = OptionsReader.GetAttribute (nav, "tolerance");

			var assembly = new Assembly (name, size, tolerance);

			nav.ProcessChildren ("namespace", assembly.Namespaces, OnNamespaceEntry);

			return assembly;
		}

		static bool GetName (XPathNavigator nav, out string name, out MatchKind match)
		{
			name = nav.GetAttribute ("name");
			var fullname = nav.GetAttribute ("fullname");
			var substring = nav.GetAttribute ("substring");

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
	}
}
