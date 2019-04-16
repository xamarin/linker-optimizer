//
// OldOptimizerReport.cs
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
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using Mono.Cecil;

namespace Mono.Linker.Optimizer
{
	using BasicBlocks;
	using Configuration;

	[Obsolete]
	public abstract class OldOptimizerReport
	{
		public OptimizerOptions Options {
			get;
		}

		public ILogger Logger {
			get;
			private set;
		}

		public ReportMode Mode => Options.ReportMode;

		public bool IsEnabled (ReportMode mode) => (Mode & mode) != 0;

		RootEntry Root { get; } = new RootEntry ();

		public OldOptimizerReport (OptimizerOptions options)
		{
			Options = options;
		}

		public void Initialize (OptimizerContext context)
		{
			Logger = context.Context.Logger;

			if (Root.Initialize (Options.ReportConfiguration, Options.ReportProfile, Options.CheckSize))
				return;

			if (Options.CheckSize)
				LogWarning ($"Cannot find size entries for configuration `{Options.ReportConfiguration}`, profile `{Options.ReportProfile}`.");
		}

		public void Read (XPathNavigator nav)
		{
			var name = OptionsReader.GetAttribute (nav, "configuration");
			var configuration = Root.GetConfiguration (name, true);

			OptionsReader.ProcessChildren (nav, "profile", child => OnProfileEntry (child, configuration));
		}

		public bool CheckAndReportAssemblySize (OptimizerContext context, AssemblyDefinition assembly, int size)
		{
			ReportAssemblySize (context, assembly, size);

			return CheckAssemblySize (assembly.Name.Name, size);
		}

		void LogMessage (string message)
		{
			Logger.LogMessage (MessageImportance.Normal, message);
		}

		void LogWarning (string message)
		{
			Logger.LogMessage (MessageImportance.High, message);
		}

		[Conditional ("DEBUG")]
		void LogDebug (string message)
		{
			Logger.LogMessage (MessageImportance.Low, message);
		}

		void OnProfileEntry (XPathNavigator nav, ConfigurationEntry configuration)
		{
			var profile = OptionsReader.GetAttribute (nav, "name") ?? throw OptionsReader.ThrowError ("<profile> requires `name` attribute.");

			var entry = new ProfileEntry (profile);
			configuration.ProfileEntries.Add (entry);

			OptionsReader.ProcessChildren (nav, "assembly", child => OnAssemblyEntry (child, entry));
		}

		void OnAssemblyEntry (XPathNavigator nav, ProfileEntry entry)
		{
			var name = OptionsReader.GetAttribute (nav, "name") ?? throw OptionsReader.ThrowError ("<assembly> requires `name` attribute.");
			var sizeAttr = OptionsReader.GetAttribute (nav, "size");
			if (sizeAttr == null || !int.TryParse (sizeAttr, out var size))
				throw OptionsReader.ThrowError ("<assembly> requires `size` attribute.");
			var toleranceAttr = OptionsReader.GetAttribute (nav, "tolerance");

			var assembly = new AssemblyEntry (name, size, toleranceAttr);
			entry.Assemblies.Add (assembly);

			OptionsReader.ProcessChildren (nav, "namespace", child => OnNamespaceEntry (child, assembly));
		}

		void OnNamespaceEntry (XPathNavigator nav, AssemblyEntry assembly)
		{
			var name = OptionsReader.GetAttribute (nav, "name") ?? throw OptionsReader.ThrowError ("<namespace> requires `name` attribute.");

			var ns = assembly.GetNamespace (name);

			OptionsReader.ProcessChildren (nav, "type", child => OnTypeEntry (child, ns));
		}

		void OnTypeEntry (XPathNavigator nav, NamespaceEntry parent)
		{
			var name = OptionsReader.GetAttribute (nav, "name") ?? throw OptionsReader.ThrowError ("<type> requires `name` attribute.");
			var fullName = OptionsReader.GetAttribute (nav, "full-name") ?? throw OptionsReader.ThrowError ("<type> requires `full-name` attribute.");
			parent.GetType (name, fullName);
		}

		AssemblyEntry GetAssemblyEntry (string name, bool add)
		{
			return Root.AssemblyList.GetAssembly (name, add);
		}

		bool CheckAssemblySize (string assembly, int size)
		{
			if (!Options.CheckSize)
				return true;

			var asmEntry = GetAssemblyEntry (assembly, false);
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

			LogDebug ($"Size check: {asmEntry.Name}, actual={size}, expected={asmEntry.Size} (tolerance {toleranceValue})");

			if (size < asmEntry.Size - tolerance) {
				LogWarning ($"Assembly `{asmEntry.Name}` size below minimum: expected {asmEntry.Size} (tolerance {toleranceValue}), got {size}.");
				return false;
			}
			if (size > asmEntry.Size + tolerance) {
				LogWarning ($"Assembly `{asmEntry.Name}` size above maximum: expected {asmEntry.Size} (tolerance {toleranceValue}), got {size}.");
				return false;
			}

			return true;
		}

		void ReportAssemblySize (OptimizerContext context, AssemblyDefinition assembly, int size)
		{
			var asmEntry = GetAssemblyEntry (assembly.Name.Name, true);
			asmEntry.SetSize (size);

			ReportDetailed (context, assembly, asmEntry);
		}

		void ReportDetailed (OptimizerContext context, AssemblyDefinition assembly, AssemblyEntry entry)
		{
			foreach (var type in assembly.MainModule.Types) {
				ProcessType (context, entry, type);
			}
		}

		void CompareSize (XmlWriter xml, AssemblyEntry assembly)
		{
			xml.WriteStartElement ("removed-types");
			foreach (var ns in assembly.GetNamespaces ()) {
				if (string.IsNullOrEmpty (ns.Name))
					continue;
				CompareSize (xml, ns);
			}
			xml.WriteEndElement ();
		}

		void CompareSize (XmlWriter xml, NamespaceEntry entry)
		{
			var types = entry.GetTypes ();
			if (entry.Marked && types.All (t => t.Marked))
				return;

			xml.WriteStartElement ("namespace");
			xml.WriteAttributeString ("name", entry.Name);
			if (!entry.Marked)
				xml.WriteAttributeString ("action", "fail");

			foreach (var type in types) {
				if (type.Marked)
					continue;
				xml.WriteStartElement ("type");
				xml.WriteAttributeString ("name", type.Name);
				xml.WriteAttributeString ("action", "fail");
				xml.WriteEndElement ();
			}

			xml.WriteEndElement ();
		}

		void ProcessType (OptimizerContext context, AssemblyEntry parent, TypeDefinition type)
		{
			if (type.Name == "<Module>")
				return;
			if (!context.Annotations.IsMarked (type))
				throw DebugHelpers.AssertFail ($"Type `{type}` is not marked.");
			if (type.FullName.StartsWith ("<PrivateImplementationDetails>", StringComparison.Ordinal))
				return;

			var ns = parent.GetNamespace (type.Namespace);
			ProcessType (context, ns, type);
		}

		void ProcessType (OptimizerContext context, AbstractTypeEntry parent, TypeDefinition type)
		{
			if (!context.Annotations.IsMarked (type))
				throw DebugHelpers.AssertFail ($"Type `{type}` is not marked.");

			parent.Marked = true;

			var entry = parent.GetType (type, true);
			entry.Marked = true;

			foreach (var method in type.Methods)
				ProcessMethod (context, entry, method);

			foreach (var nested in type.NestedTypes)
				ProcessType (context, entry, nested);
		}

		void ProcessMethod (OptimizerContext context, TypeEntry parent, MethodDefinition method)
		{
			if (!method.HasBody)
				return;
			if (!context.Annotations.IsMarked (method))
				throw DebugHelpers.AssertFail ($"Method `{method}` is not marked.");

			if (!parent.AddMethod (method))
				return;
		}

		interface IVisitor
		{
			void Visit (RootEntry entry);

			void Visit (SizeReportEntry entry);

			void Visit (ConfigurationEntry entry);

			void Visit (ProfileEntry entry);

			void Visit (AssemblyEntry entry);

			void Visit (NamespaceEntry entry);

			void Visit (TypeEntry entry);

			void Visit (MethodEntry entry);
		}

		enum WriteMode
		{
			Root,
			Size,
			Detailed,
			Action
		}

		abstract class AbstractReportEntry
		{
			public abstract string ElementName {
				get;
			}

			public abstract void WriteElement (XmlWriter xml);

			public abstract void Visit (IVisitor visitor);

			public abstract void VisitChildren (IVisitor visitor);
		}

		class RootEntry : AbstractReportEntry
		{
			public override string ElementName => "optimizer-report";

			public List<ConfigurationEntry> ConfigurationEntries { get; } = new List<ConfigurationEntry> ();

			public ProfileEntry DefaultProfile {
				get;
				private set;
			}

			public AssemblyListEntry AssemblyList {
				get;
				private set;
			}

			public SizeReportEntry SizeReport {
				get;
				private set;
			}

			public bool Initialize (string configuration, string profile, bool add)
			{
				DefaultProfile = GetProfile (configuration, profile, add);
				if (DefaultProfile != null) {
					AssemblyList = DefaultProfile;
					return true;
				}

				SizeReport = new SizeReportEntry ();
				AssemblyList = SizeReport;
				return false;
			}

			public ConfigurationEntry GetConfiguration (string configuration, bool add)
			{
				var entry = ConfigurationEntries.FirstOrDefault (e => e.Configuration == configuration);
				if (add && entry == null) {
					entry = new ConfigurationEntry (configuration);
					ConfigurationEntries.Add (entry);
				}
				return entry;
			}

			public ProfileEntry GetProfile (string configuration, string profile, bool add)
			{
				return GetConfiguration (configuration, add)?.GetProfile (profile, add);
			}

			public override void WriteElement (XmlWriter xml)
			{
			}

			public override void Visit (IVisitor visitor)
			{
				visitor.Visit (this);
			}

			public override void VisitChildren (IVisitor visitor)
			{
				ConfigurationEntries.ForEach (configuration => configuration.Visit (visitor));
				SizeReport?.Visit (visitor);
			}
		}

		class ConfigurationEntry : AbstractReportEntry
		{
			public string Configuration {
				get;
			}

			public List<ProfileEntry> ProfileEntries { get; } = new List<ProfileEntry> ();

			public override string ElementName => "configuration";

			public ProfileEntry GetProfile (string profile, bool add)
			{
				var entry = ProfileEntries.FirstOrDefault (e => e.Profile == profile);
				if (add && entry == null) {
					entry = new ProfileEntry (profile);
					ProfileEntries.Add (entry);
				}
				return entry;
			}

			public ConfigurationEntry (string configuration)
			{
				Configuration = configuration;
				ProfileEntries = new List<ProfileEntry> ();
			}

			public override void WriteElement (XmlWriter xml)
			{
				if (!string.IsNullOrEmpty (Configuration))
					xml.WriteAttributeString ("name", Configuration);
			}

			public override void Visit (IVisitor visitor)
			{
				visitor.Visit (this);
			}

			public override void VisitChildren (IVisitor visitor)
			{
				ProfileEntries.ForEach (profile => profile.Visit (visitor));
			}
		}

		abstract class AssemblyListEntry : AbstractReportEntry
		{
			public List<AssemblyEntry> Assemblies { get; } = new List<AssemblyEntry> ();

			public AssemblyEntry GetAssembly (string assembly, bool add)
			{
				var entry = Assemblies.FirstOrDefault (e => e.Name == assembly);
				if (add && entry == null) {
					entry = new AssemblyEntry (assembly, 0, null);
					Assemblies.Add (entry);
				}
				return entry;
			}

			public sealed override void VisitChildren (IVisitor visitor)
			{
				Assemblies.ForEach (assembly => assembly.Visit (visitor));
			}
		}

		class SizeReportEntry : AssemblyListEntry
		{
			public override string ElementName => "size-report";

			public override void WriteElement (XmlWriter xml)
			{
			}

			public override void Visit (IVisitor visitor)
			{
				visitor.Visit (this);
			}
		}

		class ProfileEntry : AssemblyListEntry
		{
			public string Profile {
				get;
			}

			public override string ElementName => "profile";

			public ProfileEntry (string profile)
			{
				Profile = profile;
			}

			public override void WriteElement (XmlWriter xml)
			{
				if (!string.IsNullOrEmpty (Profile))
					xml.WriteAttributeString ("name", Profile);
			}

			public override void Visit (IVisitor visitor)
			{
				visitor.Visit (this);
			}
		}

		abstract class ReportEntry : AbstractReportEntry, IComparable<ReportEntry>
		{
			public ReportEntry Parent {
				get;
			}

			public string Name {
				get;
			}

			public int Size {
				get;
				protected set;
			}

			public bool Marked {
				get; set;
			}

			void AddSize (int size)
			{
				Size += size;
				if (Parent is AbstractTypeEntry parent)
					parent.AddSize (size);
			}

			protected ReportEntry (ReportEntry parent, string name, int size)
			{
				Parent = parent;
				Name = name;

				AddSize (size);
			}

			public int CompareTo (ReportEntry obj)
			{
				return Size.CompareTo (obj.Size);
			}

			public override void WriteElement (XmlWriter xml)
			{
				xml.WriteAttributeString ("name", Name);
				if (Size != 0)
					xml.WriteAttributeString ("size", Size.ToString ());
			}

			public override string ToString ()
			{
				return $"[{GetType ().Name}: {Name} {Size}]";
			}
		}

		class AssemblyEntry : ReportEntry
		{
			public string Tolerance {
				get;
			}

			public override string ElementName => "assembly";

			internal void SetSize (int size)
			{
				Size = size;
			}

			Dictionary<string, NamespaceEntry> namespaces;

			public NamespaceEntry GetNamespace (string name, bool add = true)
			{
				LazyInitializer.EnsureInitialized (ref namespaces);
				if (namespaces.TryGetValue (name, out var entry))
					return entry;
				if (!add)
					return null;
				entry = new NamespaceEntry (this, name);
				namespaces.Add (name, entry);
				return entry;
			}

			public List<NamespaceEntry> GetNamespaces ()
			{
				var list = new List<NamespaceEntry> ();
				if (namespaces != null) {
					foreach (var ns in namespaces.Values)
						list.Add (ns);
					list.Sort ();
				}
				return list;
			}

			public override void WriteElement (XmlWriter xml)
			{
				if (!string.IsNullOrEmpty (Tolerance))
					xml.WriteAttributeString ("tolerance", Tolerance);
				base.WriteElement (xml);
			}

			public AssemblyEntry (string name, int size, string tolerance)
				: base (null, name, size)
			{
				Tolerance = tolerance;
			}

			public override void Visit (IVisitor visitor)
			{
				visitor.Visit (this);
			}

			public override void VisitChildren (IVisitor visitor)
			{
				GetNamespaces ().ForEach (ns => ns.Visit (visitor));
			}

			public override string ToString ()
			{
				return $"[{GetType ().Name}: {Name} {Size} {Tolerance}]";
			}
		}

		abstract class AbstractTypeEntry : ReportEntry
		{
			protected AbstractTypeEntry (ReportEntry parent, string name)
				: base (parent, name, 0)
			{
			}

			Dictionary<string, TypeEntry> types;

			public bool HasTypes => types != null && types.Count > 0;

			public TypeEntry GetType (TypeDefinition type, bool add = true)
			{
				return GetType (type.Name, type.FullName, add);
			}

			public TypeEntry GetType (string name, string fullName, bool add = true)
			{
				LazyInitializer.EnsureInitialized (ref types);
				if (types.TryGetValue (name, out var entry))
					return entry;
				if (!add)
					return null;
				entry = new TypeEntry (this, name, fullName);
				types.Add (name, entry);
				return entry;
			}

			public List<TypeEntry> GetTypes ()
			{
				var list = new List<TypeEntry> ();
				if (types != null) {
					foreach (var type in types.Values)
						list.Add (type);
					list.Sort ();
				}
				return list;
			}

			public override void VisitChildren (IVisitor visitor)
			{
				GetTypes ().ForEach (ns => ns.Visit (visitor));
			}
		}

		class NamespaceEntry : AbstractTypeEntry
		{
			public override string ElementName => "namespace";

			public NamespaceEntry (AssemblyEntry parent, string name)
				: base (parent, name)
			{
			}

			public override void Visit (IVisitor visitor)
			{
				visitor.Visit (this);
			}
		}

		class TypeEntry : AbstractTypeEntry
		{
			public override string ElementName => "type";

			public bool AddMethod (MethodDefinition method)
			{
				LazyInitializer.EnsureInitialized (ref methods);

				var name = method.Name + CecilHelper.GetMethodSignature (method);
				if (methods.ContainsKey (name))
					return false;

				methods.Add (name, new MethodEntry (this, name, method.Body.CodeSize));
				return true;
			}

			public MethodEntry GetMethod (MethodDefinition method, bool add)
			{
				LazyInitializer.EnsureInitialized (ref methods);

				var name = method.Name + CecilHelper.GetMethodSignature (method);
				if (methods.TryGetValue (name, out var entry))
					return entry;
				if (!add)
					return null;
				entry = new MethodEntry (this, name, method.Body.CodeSize);
				methods.Add (name, entry);
				return entry;
			}

			public List<MethodEntry> GetMethods ()
			{
				var list = new List<MethodEntry> ();
				if (methods != null) {
					foreach (var method in methods.Values)
						list.Add (method);
					list.Sort ();
				}
				return list;
			}

			Dictionary<string, MethodEntry> methods;

			public string FullName {
				get;
			}

			public TypeEntry (ReportEntry parent, string name, string fullName)
				: base (parent, name)
			{
				FullName = fullName;
			}

			public override void WriteElement (XmlWriter xml)
			{
				base.WriteElement (xml);
				if (!string.IsNullOrEmpty (FullName))
					xml.WriteAttributeString ("full-name", FullName);
			}

			public override void Visit (IVisitor visitor)
			{
				visitor.Visit (this);
			}

			public override void VisitChildren (IVisitor visitor)
			{
				base.VisitChildren (visitor);
				GetMethods ().ForEach (method => method.Visit (visitor));
			}
		}

		class MethodEntry : ReportEntry
		{
			public override string ElementName => "method";

			public bool HasAction {
				get; set;
			}

			public MethodEntry (TypeEntry parent, string name, int size)
				: base (parent, name, size)
			{
			}

			public override void Visit (IVisitor visitor)
			{
				visitor.Visit (this);
			}

			public override void VisitChildren (IVisitor visitor)
			{
			}
		}
	}
}
