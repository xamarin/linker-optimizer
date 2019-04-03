//
// ConstantCallConditional.cs
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
using Mono.Cecil.Cil;

namespace Mono.Linker.Optimizer.Conditionals
{
	using BasicBlocks;

	public class ConstantCallConditional : LinkerConditional
	{
		public MethodDefinition Target {
			get;
		}

		public int StackDepth {
			get;
		}

		public ConstantValue Constant {
			get;
		}

		ConstantCallConditional (BasicBlockScanner scanner, MethodDefinition target, int stackDepth, ConstantValue constant)
			: base (scanner)
		{
			Target = target;
			Constant = constant;
			StackDepth = stackDepth;
		}

		public override void RewriteConditional (ref BasicBlock block)
		{
			Scanner.LogDebug (1, $"REWRITE CONSTANT CALL: {this}");

			RewriteConditional (ref block, StackDepth, Constant);
		}

		public static ConstantCallConditional Create (BasicBlockScanner scanner, ref BasicBlock bb, ref int index, MethodDefinition target, ConstantValue constant)
		{
			if (index + 1 >= scanner.Body.Instructions.Count)
				throw new OptimizerAssertionException ();

			/*
			 * Calling a constant property.
			 *
			 */

			int stackDepth;
			int size = 1;
			if (target.IsStatic)
				stackDepth = 0;
			else if (bb.Count == 1)
				stackDepth = 1;
			else {
				var previous = scanner.Body.Instructions [index - 1];
				if (CecilHelper.IsSimpleLoad (previous) || previous.OpCode.Code == Code.Nop) {
					stackDepth = 0;
					size++;
				} else {
					stackDepth = 1;
				}
			}

			if (bb.Instructions.Count > size)
				scanner.BlockList.SplitBlockAt (ref bb, bb.Instructions.Count - size);

			var instance = new ConstantCallConditional (scanner, target, stackDepth, constant);
			bb.LinkerConditional = instance;

			LookAheadAfterConditional (scanner.BlockList, ref bb, ref index);

			return instance;
		}

		public override string ToString ()
		{
			return $"[{GetType ().Name}: {StackDepth} {Constant} {Target}]";
		}
	}
}
