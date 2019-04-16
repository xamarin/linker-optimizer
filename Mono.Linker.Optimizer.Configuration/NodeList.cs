//
// NodeList.cs
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

namespace Mono.Linker.Optimizer.Configuration
{
	public class NodeList<T>
		where T : Node
	{
		public bool IsEmpty => children == null || children.Count == 0;

		public List<T> Children => children;

		List<T> children;

		public T GetChild (Func<T, bool> func, Func<T> create) => GetChild (func, create != null, create);

		public T GetChild (Func<T, bool> func, bool add, Func<T> create)
		{
			LazyInitializer.EnsureInitialized (ref children);
			var child = children.FirstOrDefault (func);
			if (child != null)
				return child;
			if (child == null && add) {
				child = create ();
				if (child != null)
					children.Add (child);
			}
			return child;
		}

		public void Add (T item)
		{
			LazyInitializer.EnsureInitialized (ref children);
			children.Add (item);
		}

		public void VisitChildren (IVisitor visitor)
		{
			children?.ForEach (child => child.Visit (visitor));
		}
	}
}
