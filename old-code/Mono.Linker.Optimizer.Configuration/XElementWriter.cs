//
// XElementWriter.cs
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
using System.Xml.Linq;
using System.Collections.Generic;

namespace Mono.Linker.Optimizer.Configuration
{
	public abstract class XElementWriter : IVisitor
	{
		public XNode Root {
			get;
		}

		Stack<CurrentNode> Stack { get; } = new Stack<CurrentNode> ();

		class CurrentNode
		{
			public XNode Node {
				get;
			}

			public CurrentNode (XNode node)
			{
				Node = node;
			}
		}

		protected XElementWriter (XNode root)
		{
			Root = root;
			Stack.Push (new CurrentNode (root));
		}

		void Visit<T> (T node, string elementName, Func<T, XElement, bool> func)
			where T : Node
		{
			var element = new XElement (elementName);
			var current = new CurrentNode (element);
			Stack.Push (current);

			if (func (node, element))
				node.VisitChildren (this);

			Stack.Pop ();

			var parent = Stack.Peek ();
			if (parent.Node is XDocument document)
				document.Add (current.Node);
			else
				((XElement)parent.Node).Add (current.Node);
		}

		void IVisitor.Visit (OptimizerConfiguration node) => Visit (node, "root", Visit);

		void IVisitor.Visit (ActionList node) => Visit (node, "action-list", Visit);

		void IVisitor.Visit (SizeReport node) => Visit (node, "size-report", Visit);

		void IVisitor.Visit (SizeCheck node) => Visit (node, "size-check", Visit);

		void IVisitor.Visit (SizeComparision node) => Visit (node, "size-comparision", Visit);

		void IVisitor.Visit (OptimizerReport node) => Visit (node, "optimizer-report", Visit);

		void IVisitor.Visit (Configuration node) => Visit (node, "configuration", Visit);

		void IVisitor.Visit (Profile node) => Visit (node, "profile", Visit);

		void IVisitor.Visit (Assembly node) => Visit (node, "assembly", Visit);

		void IVisitor.Visit (Type node) => Visit (node, node.Match == MatchKind.Namespace ? "namespace" : "type", Visit);

		void IVisitor.Visit (Method node) => Visit (node, "method", Visit);

		void IVisitor.Visit (FailList node) => Visit (node, "fail-list", Visit);

		void IVisitor.Visit (FailListEntry node) => Visit (node, node.IsFatal ? "warn" : "fail", Visit);

		void IVisitor.Visit (FailListNode node) => Visit (node, node.ElementName, Visit);

		protected abstract bool Visit (OptimizerConfiguration node, XElement element);

		protected abstract bool Visit (SizeReport node, XElement element);

		protected abstract bool Visit (SizeCheck node, XElement element);

		protected abstract bool Visit (SizeComparision node, XElement element);

		protected abstract bool Visit (OptimizerReport node, XElement element);

		protected abstract bool Visit (ActionList node, XElement element);

		protected abstract bool Visit (Configuration node, XElement element);

		protected abstract bool Visit (Profile node, XElement element);

		protected abstract bool Visit (Assembly node, XElement element);

		protected abstract bool Visit (Type node, XElement element);

		protected abstract bool Visit (Method node, XElement element);

		protected abstract bool Visit (FailList node, XElement element);

		protected abstract bool Visit (FailListEntry node, XElement element);

		protected abstract bool Visit (FailListNode node, XElement element);

		protected static void SetName (XElement element, string name, MatchKind match)
		{
			switch (match) {
			case MatchKind.Substring:
				element.SetAttributeValue ("substring", name);
				break;
			case MatchKind.FullName:
				element.SetAttributeValue ("fullname", name);
				break;
			default:
				element.SetAttributeValue ("name", name);
				break;
			}
		}

		protected static void SetTypeAction (XElement element, TypeAction action)
		{
			switch (action) {
			case TypeAction.None:
				break;
			case TypeAction.Debug:
				element.SetAttributeValue ("action", "debug");
				break;
			case TypeAction.Warn:
				element.SetAttributeValue ("action", "warn");
				break;
			case TypeAction.Fail:
				element.SetAttributeValue ("action", "fail");
				break;
			case TypeAction.Preserve:
				element.SetAttributeValue ("action", "preserve");
				break;
			case TypeAction.Size:
				element.SetAttributeValue ("action", "size");
				break;
			default:
				throw DebugHelpers.AssertFail ($"Invalid type action: `{action}`.");
			}
		}

		protected static void SetMethodAction (XElement element, MethodAction action)
		{
			switch (action) {
			case MethodAction.None:
				break;
			case MethodAction.Scan:
				element.SetAttributeValue ("action", "scan");
				break;
			case MethodAction.Debug:
				element.SetAttributeValue ("action", "debug");
				break;
			case MethodAction.Warn:
				element.SetAttributeValue ("action", "warn");
				break;
			case MethodAction.Fail:
				element.SetAttributeValue ("action", "fail");
				break;
			case MethodAction.ReturnFalse:
				element.SetAttributeValue ("action", "return-false");
				break;
			case MethodAction.ReturnTrue:
				element.SetAttributeValue ("action", "return-true");
				break;
			case MethodAction.ReturnNull:
				element.SetAttributeValue ("action", "return-null");
				break;
			case MethodAction.Throw:
				element.SetAttributeValue ("action", "throw");
				break;
			default:
				throw DebugHelpers.AssertFail ($"Invalid method action: `{action}`.");
			}
		}

		protected static void SetDeadCodeMode (XElement element, DeadCodeMode mode)
		{
			if (mode == DeadCodeMode.None)
				return;

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

			element.SetAttributeValue ("dead-code", string.Join (",", modes));
		}
	}
}
