//
// ActionList.cs
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
using System.Threading;
using System.Collections.Generic;
using Mono.Cecil;

namespace Mono.Linker.Optimizer.Configuration
{
	public class ActionList : Node
	{
		public string Conditional {
			get;
		}

		public bool Enabled {
			get;
		}

		readonly List<Node> children = new List<Node> ();
		bool? evaluated;

		public ActionList ()
		{
		}

		public ActionList (string conditional, bool enabled)
		{
			Conditional = conditional;
			Enabled = enabled;
		}

		public bool Evaluate (OptimizerOptions options)
		{
			if (evaluated == null)
				evaluated = Conditional == null || options.IsFeatureEnabled (Conditional) == Enabled;
			return evaluated.Value;
		}

		public void Add (ActionList node) => children.Add (node);

		public void Add (Type node) => children.Add (node);

		public void Add (Method node) => children.Add (node);

		public Type GetNamespace (string name, bool add)
		{
			var ns = children.OfType<Type> ().FirstOrDefault (t => t.Match == MatchKind.Namespace && t.Name == name);
			if (add && ns == null) {
				ns = new Type (null, name, null, MatchKind.Namespace, TypeAction.None);
				children.Add (ns);
			}
			return ns;
		}

		public Type GetType (TypeDefinition type, bool add)
		{
			Type parent;
			if (type.DeclaringType != null)
				parent = GetType (type.DeclaringType, add);
			else
				parent = GetNamespace (type.Namespace, add);

			return parent?.Types.GetType (parent, type, add);
		}

		public Method GetMethod (MethodDefinition method, bool add = true, MethodAction? action = null)
		{
			var parent = GetType (method.DeclaringType, add);
			return parent?.Methods.GetChild (
				m => m.Matches (method, action), add, () => new Method (parent, method, action));
		}

		public Method AddMethod (MethodDefinition method, MethodAction action)
		{
			var parent = GetType (method.DeclaringType, true);
			var entry = new Method (parent, method, action);
			parent.Methods.Add (entry);
			return entry;
		}

		public override void Visit (IVisitor visitor)
		{
			visitor.Visit (this);
		}

		public override void VisitChildren (IVisitor visitor)
		{
			children.ForEach (node => node.Visit (visitor));
		}
	}
}
