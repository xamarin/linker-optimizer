//
// IsFeatureSupportedConditional.cs
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
using System.Runtime.CompilerServices;

namespace Mono.Linker.Optimizer.Conditionals
{
	public class IsFeatureSupportedConditional : LinkerConditional
	{
		public MonoLinkerFeature Feature {
			get;
		}

		IsFeatureSupportedConditional (BasicBlockScanner scanner, MonoLinkerFeature feature)
			: base (scanner)
		{
			Feature = feature;
		}

		public override void RewriteConditional (ref BasicBlock block)
		{
			var evaluated = Context.Options.IsFeatureEnabled (Feature);
			Scanner.LogDebug (1, $"REWRITE FEATURE CONDITIONAL: {Feature} {evaluated}");

			RewriteConditional (ref block, 0, evaluated ? ConstantValue.True : ConstantValue.False);
		}

		public static IsFeatureSupportedConditional Create (BasicBlockScanner scanner, ref BasicBlock bb, ref int index)
		{
			if (bb.Instructions.Count == 1)
				throw new NotSupportedException ();
			if (index + 1 >= scanner.Body.Instructions.Count)
				throw new NotSupportedException ();

			/*
			 * `bool MonoLinkerSupport.IsFeatureSupported (MonoLinkerFeature feature)`
			 *
			 */

			if (bb.Instructions.Count > 2)
				scanner.BlockList.SplitBlockAt (ref bb, bb.Instructions.Count - 2);

			var feature = (MonoLinkerFeature)CecilHelper.GetFeatureArgument (bb.FirstInstruction);
			var instance = new IsFeatureSupportedConditional (scanner, feature);
			bb.LinkerConditional = instance;

			LookAheadAfterConditional (scanner.BlockList, ref bb, ref index);

			return instance;
		}

		public override string ToString ()
		{
			return $"[{GetType ().Name}: {Feature}]";
		}
	}
}
