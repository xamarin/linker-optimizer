//
// IsWeakInstanceOfConditional.cs
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

namespace Mono.Linker.Optimizer
{
	public class IsWeakInstanceOfConditional : LinkerConditional
	{
		public TypeDefinition InstanceType {
			get;
		}

		public bool HasLoadInstruction {
			get;
		}

		IsWeakInstanceOfConditional (BasicBlockScanner scanner, TypeDefinition type, bool hasLoad)
			: base (scanner)
		{
			InstanceType = type;
			HasLoadInstruction = hasLoad;
		}

		public override void RewriteConditional (ref BasicBlock block)
		{
			Scanner.LogDebug (1, $"REWRITE CONDITIONAL: {this} {BlockList.Body.Method.Name} {block}");

			var evaluated = Context.Annotations.IsMarked (InstanceType);
			Context.MarkConditionalType (InstanceType);

			if (!evaluated)
				RewriteConditional (ref block, HasLoadInstruction ? 0 : 1, ConstantValue.False);
			else
				Scanner.Rewriter.ReplaceWithIsInst (ref block, HasLoadInstruction ? 1 : 0, InstanceType);
		}

		public static IsWeakInstanceOfConditional Create (BasicBlockScanner scanner, ref BasicBlock bb, ref int index, TypeDefinition type)
		{
			if (bb.Instructions.Count == 1)
				throw new NotSupportedException ();
			if (index + 1 >= scanner.Body.Instructions.Count)
				throw new NotSupportedException ();

			/*
			 * `bool MonoLinkerSupport.IsWeakInstance<T> (object instance)`
			 *
			 * If the function argument is a simple load (like for instance `Ldarg_0`),
			 * then we can simply remove that load.  Otherwise, we need to insert a
			 * `Pop` to discard the value on the stack.
			 *
			 * In either case, we always start a new basic block for the conditional.
			 * Its first instruction will either be the simple load or the call itself.
			 */

			var argument = scanner.Body.Instructions [index - 1];

			scanner.LogDebug (1, $"WEAK INSTANCE OF: {bb} {index} {type} - {argument}");

			bool hasLoad;
			TypeDefinition instanceType;
			if (CecilHelper.IsSimpleLoad (argument)) {
				if (bb.Instructions.Count > 2)
					scanner.BlockList.SplitBlockAt (ref bb, bb.Instructions.Count - 2);
				instanceType = CecilHelper.GetWeakInstanceArgument (bb.Instructions [1]);
				hasLoad = true;
			} else {
				scanner.BlockList.SplitBlockAt (ref bb, bb.Instructions.Count - 1);
				instanceType = CecilHelper.GetWeakInstanceArgument (bb.Instructions [0]);
				hasLoad = false;
			}

			var instance = new IsWeakInstanceOfConditional (scanner, instanceType, hasLoad);
			bb.LinkerConditional = instance;

			/*
			 * Once we get here, the current block only contains the (optional) simple load
			 * and the conditional call itself.
			 */

			LookAheadAfterConditional (scanner.BlockList, ref bb, ref index);

			return instance;
		}

		public override string ToString ()
		{
			return $"[{GetType ().Name}: {InstanceType.Name} {HasLoadInstruction}]";
		}
	}
}
