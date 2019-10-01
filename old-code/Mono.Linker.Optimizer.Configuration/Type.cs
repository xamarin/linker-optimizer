//
// Type.cs
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
using Mono.Cecil;

namespace Mono.Linker.Optimizer.Configuration
{
	public class Type : Node
	{
		public Type Parent {
			get;
		}

		public string Name {
			get;
		}

		public string FullName {
			get;
		}
		public MatchKind Match {
			get;
		}

		public TypeAction? Action {
			get;
		}

		public int? Size {
			get; set;
		}

		public NodeList<Type> Types { get; } = new NodeList<Type> ();

		public NodeList<Method> Methods { get; } = new NodeList<Method> ();

		public bool Matches (TypeDefinition type, TypeAction? action = null)
		{
			if (action != null && Action != null && action.Value != Action)
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

		public Type (Type parent, string name, string fullName, MatchKind match, TypeAction? action = null)
		{
			Parent = parent;
			Name = name;
			FullName = fullName;
			Match = match;
			Action = action;
		}

		public override void Visit (IVisitor visitor)
		{
			visitor.Visit (this);
		}

		public sealed override void VisitChildren (IVisitor visitor)
		{
			Types.VisitChildren (visitor);
			Methods.VisitChildren (visitor);
		}

		public override string ToString ()
		{
			return $"[{GetType ().Name} {Name} {Match} {Action}]";
		}
	}
}
