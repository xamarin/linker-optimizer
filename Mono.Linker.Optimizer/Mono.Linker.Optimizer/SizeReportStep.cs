//
// SizeReportStep.cs
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
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Mono.Cecil;

namespace Mono.Linker.Optimizer
{
	public class SizeReportStep : OptimizerBaseStep
	{
		public SizeReportStep (OptimizerContext context)
			: base (context)
		{
		}

		protected override void Process ()
		{
			if (!Options.CheckSize && !Options.OptimizerReport.IsEnabled (ReportMode.Size))
				return;

			bool result = true;
			foreach (var assembly in GetAssemblies ()) {
				result &= CheckAndReportSize (assembly);
			}

			Context.SizeCheckFailed |= !result;
		}

		bool CheckAndReportSize (AssemblyDefinition assembly)
		{
			var action = Annotations.GetAction (assembly);
			switch (action) {
			case AssemblyAction.Save:
			case AssemblyAction.Link:
			case AssemblyAction.AddBypassNGen:
			case AssemblyAction.Copy:
				break;
			default:
				return true;
			}

			var file = new FileInfo (assembly.MainModule.FileName).Name;
			var output = Path.Combine (Context.Context.OutputDirectory, file);
			if (!File.Exists (output)) {
				Context.LogMessage (MessageImportance.High, $"Output file does not exist: {output}");
				return true;
			}

			var size = (int)new FileInfo (output).Length;
			return Options.OptimizerReport.CheckAndReportAssemblySize (Context, assembly, size);
		}
	}
}
