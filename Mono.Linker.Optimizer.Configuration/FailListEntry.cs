//
// FailListEntry.cs
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
using System.Collections.Generic;
using Mono.Cecil;

namespace Mono.Linker.Optimizer.Configuration
{
	public class FailListEntry : Node
	{
		public string FullName {
			get;
		}

		public string Original {
			get;
		}

		public bool IsFatal {
			get;
		}

		public NodeList<FailListNode> TracerStack { get; } = new NodeList<FailListNode> ();

		public NodeList<FailListNode> EntryStack { get; } = new NodeList<FailListNode> ();

		public FailListEntry (TypeDefinition type, Type entry, string original, List<string> stack)
		{
			FullName = type.FullName;
			Original = original;

			IsFatal = entry.Action == TypeAction.Fail;

			stack.ForEach (s => TracerStack.Add (new FailListNode ("stack", s)));

			while (entry != null) {
				EntryStack.Add (new FailListNode ("type", entry.ToString ()));
				entry = entry.Parent;
			}
		}

		public FailListEntry (MethodDefinition method, Method entry, List<string> stack)
		{
			FullName = method.FullName;

			IsFatal = entry.Action == MethodAction.Fail;

			stack.ForEach (s => TracerStack.Add (new FailListNode ("stack", s)));

			if (entry != null) {
				EntryStack.Add (new FailListNode ("method", entry.ToString ()));

				var type = entry.Parent;
				while (type != null) {
					EntryStack.Add (new FailListNode ("type", type.ToString ()));
					type = type.Parent;
				}
			}
		}

		public override void Visit (IVisitor visitor)
		{
			visitor.Visit (this);
		}

		public override void VisitChildren (IVisitor visitor)
		{
			TracerStack.VisitChildren (visitor);
			EntryStack.VisitChildren (visitor);
		}
	}
}
