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
using System.Xml.Linq;
using System.Collections.Generic;

namespace Mono.Linker.Optimizer.Configuration
{
	public class ReportWriter : XElementWriter
	{
		public ReportWriter (XNode root)
			: base (root)
		{
		}

		protected override bool Visit (OptimizerConfiguration node, XElement element)
		{
			return true;
		}

		protected override bool Visit (SizeReport node, XElement element)
		{
			return true;
		}

		protected override bool Visit (SizeCheck node, XElement element)
		{
			return true;
		}

		protected override bool Visit (OptimizerReport node, XElement element)
		{
			return true;
		}

		protected override bool Visit (ActionList node, XElement element)
		{
			return true;
		}

		protected override bool Visit (Configuration node, XElement element)
		{
			if (node.Name != null)
				element.SetAttributeValue ("name", node.Name);
			return true;
		}

		protected override bool Visit (Profile node, XElement element)
		{
			if (node.Name != null)
				element.SetAttributeValue ("name", node.Name);
			return true;
		}

		protected override bool Visit (Assembly node, XElement element)
		{
			element.SetAttributeValue ("name", node.Name);
			if (node.Size != null)
				element.SetAttributeValue ("size", node.Size.Value.ToString ());
			if (node.Tolerance != null)
				element.SetAttributeValue ("tolerance", node.Tolerance);
			return true;
		}

		protected override bool Visit (Type node, XElement element)
		{
			SetName (element, node.Name, node.Match);
			if (node.Size != null)
				element.SetAttributeValue ("size", node.Size.Value);
			return true;
		}

		protected override bool Visit (Method node, XElement element)
		{
			SetName (element, node.Name, node.Match);
			if (node.Action != null)
				SetMethodAction (element, node.Action.Value);
			SetDeadCodeMode (element, node.DeadCodeMode);
			if (node.Size != null)
				element.SetAttributeValue ("size", node.Size.Value);
			return true;
		}

		protected override bool Visit (FailList node, XElement element)
		{
			return true;
		}

		protected override bool Visit (FailListEntry node, XElement element)
		{
			element.SetAttributeValue ("full-name", node.FullName);
			if (node.Original != null)
				element.SetAttributeValue ("origin", node.Original);
			return true;
		}

		protected override bool Visit (FailListNode node, XElement element)
		{
			element.Add (new XCData (node.Text));
			return true;
		}
	}
}
