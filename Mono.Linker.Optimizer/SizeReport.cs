//
// SizeReport.cs
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
using System.Xml;
using System.Xml.XPath;
using System.Collections.Generic;

namespace Mono.Linker.Optimizer
{
	public class SizeReport
	{
		public OptimizerOptions Options {
			get;
		}

		readonly List<ConfigurationEntry> _configuration_entries;

		public SizeReport (OptimizerOptions options)
		{
			Options = options;

			_configuration_entries = new List<ConfigurationEntry> ();
		}

		public void Read (XPathNavigator nav)
		{
			var name = OptionsReader.GetAttribute (nav, "configuration");
			var configuration = GetConfigurationEntry (name, true);

			OptionsReader.ProcessChildren (nav, "profile", child => OnProfileEntry (child, configuration));
		}

		void OnProfileEntry (XPathNavigator nav, ConfigurationEntry configuration)
		{
			var profile = OptionsReader.GetAttribute (nav, "name") ?? throw OptionsReader.ThrowError ("<proifle> requires `name` attribute.");

			var entry = new SizeReportEntry (configuration, profile);
			configuration.SizeReportEntries.Add (entry);

			OptionsReader.ProcessChildren (nav, "assembly", child => OnAssemblyEntry (child, entry));
		}

		void OnAssemblyEntry (XPathNavigator nav, SizeReportEntry entry)
		{
			var name = OptionsReader.GetAttribute (nav, "name") ?? throw OptionsReader.ThrowError ("<assembly> requires `name` attribute.");
			var sizeAttr = OptionsReader.GetAttribute (nav, "size");
			if (sizeAttr == null || !int.TryParse (sizeAttr, out var size))
				throw OptionsReader.ThrowError ("<assembly> requires `name` attribute.");
			var toleranceAttr = OptionsReader.GetAttribute (nav, "tolerance");
			entry.Assemblies.Add (new AssemblySizeEntry (name, size, toleranceAttr));
		}

		SizeReportEntry GetSizeReportEntry (string configuration, string profile)
		{
			var configEntry = GetConfigurationEntry (configuration, false);
			if (configEntry == null)
				return null;
			return configEntry.SizeReportEntries.FirstOrDefault (e => e.Profile == profile);
		}

		ConfigurationEntry GetConfigurationEntry (string configuration, bool add)
		{
			var entry = _configuration_entries.FirstOrDefault (e => e.Configuration == configuration);
			if (add && entry == null) {
				entry = new ConfigurationEntry (configuration);
				_configuration_entries.Add (entry);
			}
			return entry;
		}

		public bool IsEnabled => SizeReportProfile != null;

		string SizeReportProfile {
			get {
				switch (Options.CheckSize) {
				case null:
				case "false":
					return null;
				case "true":
					return Options.ProfileName ?? "default";
				default:
					return Options.CheckSize;
				}
			}
		}

		public bool CheckAssemblySize (OptimizerContext context, string assembly, int size)
		{
			if (!IsEnabled)
				return true;

			var entry = GetSizeReportEntry (Options.SizeCheckConfiguration, SizeReportProfile);
			if (entry == null) {
				context.LogMessage (MessageImportance.High, $"Cannot find size entries for profile `{SizeReportProfile}`.");
				return false;
			}

			var asmEntry = entry.Assemblies.FirstOrDefault (e => e.Name == assembly);
			if (asmEntry == null)
				return true;

			int tolerance;
			string toleranceValue = asmEntry.Tolerance ?? Options.SizeCheckTolerance ?? "0.05%";

			if (toleranceValue.EndsWith ("%", StringComparison.Ordinal)) {
				var percent = float.Parse (toleranceValue.Substring (0, toleranceValue.Length - 1));
				tolerance = (int)(asmEntry.Size * percent / 100.0f);
			} else {
				tolerance = int.Parse (toleranceValue);
			}

			context.LogDebug ($"Size check: {asmEntry.Name}, actual={size}, expected={asmEntry.Size} (tolerance {toleranceValue})");

			if (size < asmEntry.Size - tolerance) {
				context.LogMessage (MessageImportance.High, $"Assembly `{asmEntry.Name}` size below minimum: expected {asmEntry.Size} (tolerance {toleranceValue}), got {size}.");
				return false;
			}
			if (size > asmEntry.Size + tolerance) {
				context.LogMessage (MessageImportance.High, $"Assembly `{asmEntry.Name}` size above maximum: expected {asmEntry.Size} (tolerance {toleranceValue}), got {size}.");
				return false;
			}

			return true;
		}

		public void ReportAssemblySize (string assembly, int size)
		{
			ReportAssemblySize (Options.SizeCheckConfiguration, SizeReportProfile, assembly, size);
		}

		public void ReportAssemblySize (string configuration, string profile, string assembly, int size)
		{
			var configEntry = GetConfigurationEntry (configuration, true);
			var sizeEntry = configEntry.SizeReportEntries.FirstOrDefault (e => e.Profile == profile);
			if (sizeEntry == null) {
				sizeEntry = new SizeReportEntry (configEntry, profile);
				configEntry.SizeReportEntries.Add (sizeEntry);
			}
			var asmEntry = sizeEntry.Assemblies.FirstOrDefault (e => e.Name == assembly);
			if (asmEntry == null) {
				asmEntry = new AssemblySizeEntry (assembly, size, null);
				sizeEntry.Assemblies.Add (asmEntry);
			} else {
				asmEntry.Size = size;
			}
		}

		public void Write (XmlWriter xml)
		{
			foreach (var configuration in _configuration_entries) {
				xml.WriteStartElement ("size-check");
				xml.WriteAttributeString ("configuration", configuration.Configuration);

				foreach (var entry in configuration.SizeReportEntries) {
					xml.WriteStartElement ("profile");
					xml.WriteAttributeString ("name", entry.Profile);
					foreach (var asm in entry.Assemblies) {
						xml.WriteStartElement ("assembly");
						xml.WriteAttributeString ("name", asm.Name);
						xml.WriteAttributeString ("size", asm.Size.ToString ());
						if (asm.Tolerance != null)
							xml.WriteAttributeString ("tolerance", asm.Tolerance);
						xml.WriteEndElement ();

					}
					xml.WriteEndElement ();
				}
				xml.WriteEndElement ();
			}
		}

		class ConfigurationEntry
		{
			public string Configuration {
				get;
			}

			public List<SizeReportEntry> SizeReportEntries {
				get;
			}

			public ConfigurationEntry (string configuration)
			{
				Configuration = configuration;
				SizeReportEntries = new List<SizeReportEntry> ();
			}
		}

		class SizeReportEntry
		{
			public ConfigurationEntry Configuration {
				get;
			}

			public string Profile {
				get;
			}

			public List<AssemblySizeEntry> Assemblies {
				get;
			}

			public SizeReportEntry (ConfigurationEntry configuration, string profile)
			{
				Configuration = configuration;
				Profile = profile;
				Assemblies = new List<AssemblySizeEntry> ();
			}
		}

		class AssemblySizeEntry
		{
			public string Name {
				get;
			}

			public int Size {
				get; set;
			}

			public string Tolerance {
				get;
			}

			public AssemblySizeEntry (string name, int size, string tolerance)
			{
				Name = name;
				Size = size;
				Tolerance = tolerance;
			}

			public override string ToString ()
			{
				return $"[{GetType ().Name}: {Name} {Size} {Tolerance}]";
			}
		}

	}
}
