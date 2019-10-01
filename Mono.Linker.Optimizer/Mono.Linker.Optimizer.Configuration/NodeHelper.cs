//
// NodeHelper.cs
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

namespace Mono.Linker.Optimizer.Configuration
{
	using BasicBlocks;

	static class NodeHelper
	{
		internal static Assembly GetAssembly (this NodeList<Assembly> list, AssemblyDefinition assembly, bool add)
		{
			return GetAssembly (list, assembly.Name.Name, add);
		}

		internal static Assembly GetAssembly (this NodeList<Assembly> list, string name, bool add)
		{
			return list.GetChild (a => a.Name == name, () => add ? new Assembly (name) : null);
		}

		internal static Type GetNamespace (this NodeList<Type> list, string name, bool add)
		{
			return list.GetChild (n => n.Name == name, () => add ? new Type (null, name, null, MatchKind.Namespace) : null);
		}

		internal static Type GetType (this NodeList<Type> list, Type parent, TypeDefinition type, bool add)
		{
			return list.GetChild (t => t.Name == type.Name, () => add ? new Type (parent, type.Name, type.FullName, MatchKind.Name) : null);
		}

		internal static Method GetMethod (this NodeList<Method> list, Type parent, MethodDefinition method, bool add)
		{
			return GetMethod (list, parent, method.Name + CecilHelper.GetMethodSignature (method), add);
		}

		internal static Method GetMethod (this NodeList<Method> list, Type parent, string name, bool add)
		{
			return list.GetChild (m => m.Name == name, () => add ? new Method (parent, name, MatchKind.Name) : null);
		}

		internal static void ProcessChildren (this XPathNavigator nav, string children, Action<XPathNavigator> action)
		{
			var iterator = nav.Select (children);
			while (iterator.MoveNext ())
				action (iterator.Current);
		}

		internal static void ProcessChildren<T> (this XPathNavigator nav, string children, NodeList<T> list, Func<XPathNavigator, T> func)
			where T : Node
		{
			var iterator = nav.Select (children);
			while (iterator.MoveNext ()) {
				var child = func (iterator.Current);
				if (child != null)
					list.Add (child);
			}
		}

		internal static void ProcessChildren<T,U> (this XPathNavigator nav, string children, T parent, NodeList<U> list, Func<XPathNavigator, T, U> func)
			where T : Node
			where U : Node
		{
			var iterator = nav.Select (children);
			while (iterator.MoveNext ()) {
				var child = func (iterator.Current, parent);
				if (child != null)
					list.Add (child);
			}
		}

		internal static bool GetBoolAttribute (this XPathNavigator nav, string name, out bool value)
		{
			var attr = GetAttribute (nav, name);
			if (attr != null && bool.TryParse (attr, out value))
				return true;
			value = false;
			return false;
		}

		internal static string GetAttribute (this XPathNavigator nav, string attribute)
		{
			var attr = nav.GetAttribute (attribute, string.Empty);
			return string.IsNullOrWhiteSpace (attr) ? null : attr;
		}

		internal static TypeAction? GetTypeAction (this XPathNavigator nav, string name)
		{
			if (TryGetTypeAction (nav, name, out var action))
				return action;
			return null;
		}

		internal static bool TryGetTypeAction (this XPathNavigator nav, string name, out TypeAction? action)
		{
			var attribute = GetAttribute (nav, name);
			if (attribute != null && Enum.TryParse (attribute, true, out TypeAction result)) {
				action = result;
				return true;
			}

			action = null;
			return false;
		}

		internal static bool TryGetMethodAction (this XPathNavigator nav, string name, out MethodAction? action)
		{
			var attribute = GetAttribute (nav, name);
			if (attribute == null) {
				action = null;
				return false;
			}

			switch (attribute.ToLowerInvariant ()) {
			case "return-null":
				action = MethodAction.ReturnNull;
				return true;
			case "return-false":
				action = MethodAction.ReturnFalse;
				return true;
			case "return-true":
				action = MethodAction.ReturnTrue;
				return true;
			}

			if (Enum.TryParse (attribute, true, out MethodAction result)) {
				action = result;
				return true;
			}

			action = null;
			return false;
		}

		internal static string GetMethodSignature (MethodDefinition method) => CecilHelper.GetMethodSignature (method);
	}
}
