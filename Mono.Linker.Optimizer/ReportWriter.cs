//
// ReportWriter.cs
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
using System.Xml;
using System.Text;
using Mono.Cecil;
using System.Collections.Generic;

namespace Mono.Linker.Optimizer
{
	using BasicBlocks;

	public class ReportWriter
	{
		public OptimizerContext Context {
			get;
		}

		readonly Dictionary<string, TypeEntry> _namespace_hash;
		readonly Dictionary<string, long> _assembly_sizes;

		public ReportWriter (OptimizerContext context)
		{
			Context = context;

			_namespace_hash = new Dictionary<string, TypeEntry> ();
			_assembly_sizes = new Dictionary<string, long> ();
		}

		public void ReportAssemblySize (AssemblyDefinition assembly, long size)
		{
			_assembly_sizes.Add (assembly.Name.Name, size);
		}

		public void MarkAsContainingConditionals (MethodDefinition method)
		{
			if (method.DeclaringType.DeclaringType != null)
				throw new NotSupportedException ($"Conditionals in nested classes are not supported yet.");

			GetMethodEntry (method);
		}

		public void MarkAsConstantMethod (MethodDefinition method, ConstantValue value)
		{
			if (method.DeclaringType.DeclaringType != null)
				throw new NotSupportedException ($"Conditionals in nested classes are not supported yet.");

			GetMethodEntry (method).ConstantValue = value;
		}

		public void RemovedDeadBlocks (MethodDefinition method)
		{
			GetMethodEntry (method).DeadCodeMode |= DeadCodeMode.RemovedDeadBlocks;
		}

		public void RemovedDeadExceptionBlocks (MethodDefinition method)
		{
			GetMethodEntry (method).DeadCodeMode |= DeadCodeMode.RemovedExceptionBlocks;
		}

		public void RemovedDeadJumps (MethodDefinition method)
		{
			GetMethodEntry (method).DeadCodeMode |= DeadCodeMode.RemovedDeadJumps;
		}

		public void RemovedDeadConstantJumps (MethodDefinition method)
		{
			GetMethodEntry (method).DeadCodeMode |= DeadCodeMode.RemovedConstantJumps;
		}

		public void RemovedDeadVariables (MethodDefinition method)
		{
			GetMethodEntry (method).DeadCodeMode |= DeadCodeMode.RemovedDeadVariables;
		}

		TypeEntry GetTypeEntry (TypeDefinition type)
		{
			if (type.DeclaringType != null)
				throw new NotSupportedException ("Nested types are not supported yet.");

			if (!_namespace_hash.TryGetValue (type.Namespace, out var entry)) {
				entry = new TypeEntry (type.Namespace);
				_namespace_hash.Add (entry.Name, entry);
			}

			if (!entry.Children.TryGetValue (type.Name, out var typeEntry)) {
				typeEntry = new TypeEntry (type.Name);
				entry.Children.Add (typeEntry.Name, typeEntry);
			}

			return typeEntry;
		}

		MethodEntry GetMethodEntry (MethodDefinition method)
		{
			var parent = GetTypeEntry (method.DeclaringType);
			if (!parent.Methods.TryGetValue (method, out var entry)) {
				entry = new MethodEntry (method.Name + CecilHelper.GetMethodSignature (method));
				parent.Methods.Add (method, entry);
			}
			return entry;
		}

		public void WriteReport (XmlWriter xml)
		{
			if (_assembly_sizes.Count > 0) {
				xml.WriteStartElement ("size-report");
				foreach (var entry in _assembly_sizes) {
					xml.WriteStartElement ("assembly");
					xml.WriteAttributeString ("name", entry.Key);
					xml.WriteAttributeString ("value", entry.Value.ToString ());
					xml.WriteEndElement ();
				}
				xml.WriteEndElement ();
			}

			foreach (var entry in _namespace_hash.Values) {
				xml.WriteStartElement ("namespace");
				xml.WriteAttributeString ("name", entry.Name);

				foreach (var type in entry.Children.Values) {
					xml.WriteStartElement ("type");
					xml.WriteAttributeString ("name", type.Name);

					foreach (var item in type.Items) {
						xml.WriteStartElement ("item");
						xml.WriteAttributeString ("name", item);
						xml.WriteAttributeString ("action", "scan");
						xml.WriteEndElement ();
					}

					foreach (var item in type.Methods.Values)
						WriteMethodEntry (xml, item);

					xml.WriteEndElement ();
				}
				xml.WriteEndElement ();
			}
		}

		void WriteMethodEntry (XmlWriter xml, MethodEntry entry)
		{
			xml.WriteStartElement ("method");
			xml.WriteAttributeString ("name", entry.Name);

			switch (entry.ConstantValue) {
			case ConstantValue.False:
				xml.WriteAttributeString ("action", "return-false");
				break;
			case ConstantValue.True:
				xml.WriteAttributeString ("action", "return-true");
				break;
			case ConstantValue.Null:
				xml.WriteAttributeString ("action", "return-null");
				break;
			case ConstantValue.Throw:
				xml.WriteAttributeString ("action", "throw");
				break;
			default:
				xml.WriteAttributeString ("action", "scan");
				break;
			}

			if (entry.DeadCodeMode != DeadCodeMode.None)
				xml.WriteAttributeString ("dead-code", FormatDeadCodeMode (entry.DeadCodeMode));

			xml.WriteEndElement ();
		}

		string FormatDeadCodeMode (DeadCodeMode mode)
		{
			if (mode == DeadCodeMode.None)
				return "none";

			var modes = new List<string> ();
			if ((mode & DeadCodeMode.RemovedDeadBlocks) != 0)
				modes.Add ("blocks");
			if ((mode & DeadCodeMode.RemovedExceptionBlocks) != 0)
				modes.Add ("exception-blocks");
			if ((mode & DeadCodeMode.RemovedDeadJumps) != 0)
				modes.Add ("jumps");
			if ((mode & DeadCodeMode.RemovedConstantJumps) != 0)
				modes.Add ("constant-jumps");
			if ((mode & DeadCodeMode.RemovedDeadVariables) != 0)
				modes.Add ("variables");
			return string.Join (",", modes);
		}

		class TypeEntry
		{
			public readonly string Name;
			public readonly Dictionary<string, TypeEntry> Children;
			public readonly Dictionary<MethodDefinition, MethodEntry> Methods;
			public readonly List<string> Items;

			public TypeEntry (string name)
			{
				Name = name;
				Children = new Dictionary<string, TypeEntry> ();
				Methods = new Dictionary<MethodDefinition, MethodEntry> ();
				Items = new List<string> ();
			}
		}

		class MethodEntry
		{
			public readonly string Name;
			public ConstantValue? ConstantValue;
			public DeadCodeMode DeadCodeMode;

			public MethodEntry (string name)
			{
				Name = name;
			}
		}

		[Flags]
		enum DeadCodeMode
		{
			None				= 0,
			RemovedDeadBlocks		= 1,
			RemovedExceptionBlocks		= 2,
			RemovedDeadJumps		= 4,
			RemovedConstantJumps		= 8,
			RemovedDeadVariables		= 16
		}
	}
}
