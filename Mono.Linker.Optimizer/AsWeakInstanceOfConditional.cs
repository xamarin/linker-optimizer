//
// AsWeakInstanceOfConditional.cs
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
	public class AsWeakInstanceOfConditional : LinkerConditional
	{
		public TypeDefinition InstanceType {
			get;
		}

		public VariableDefinition Variable {
			get;
		}

		AsWeakInstanceOfConditional (BasicBlockScanner scanner, TypeDefinition type, VariableDefinition variable)
			: base (scanner)
		{
			InstanceType = type;
			Variable = variable;
		}

		public override void RewriteConditional (ref BasicBlock block)
		{
			Scanner.LogDebug (1, $"REWRITE CONDITIONAL: {this} {BlockList.Body.Method.Name} {block}");

			var evaluated = Context.Annotations.IsMarked (InstanceType);
			Context.MarkConditionalType (InstanceType);

			if (!evaluated)
				RewriteConditional (ref block, 0, ConstantValue.False);
			else
				RewriteAsIsInst (ref block);
		}

		void RewriteAsIsInst (ref BasicBlock block)
		{
			Scanner.LogDebug (1, $"REWRITE AS ISINST: {block.Count} {block}");

			var reference = Assembly.MainModule.ImportReference (InstanceType);

			int index = 1;
			BlockList.InsertInstructionAt (ref block, index++, Instruction.Create (OpCodes.Isinst, reference));
			BlockList.InsertInstructionAt (ref block, index++, Instruction.Create (OpCodes.Dup));
			BlockList.ReplaceInstructionAt (ref block, index++, Instruction.Create (OpCodes.Stloc, Variable));
			BlockList.RemoveInstructionAt (ref block, index);

			switch (block.BranchType) {
			case BranchType.False:
			case BranchType.True:
				break;
			case BranchType.None:
			case BranchType.Return:
				// Convert it into a bool.
				BlockList.InsertInstructionAt (ref block, index++, Instruction.Create (OpCodes.Ldnull));
				BlockList.InsertInstructionAt (ref block, index++, Instruction.Create (OpCodes.Cgt_Un));
				break;
			default:
				throw DebugHelpers.AssertFailUnexpected (Method, block, block.BranchType);
			}
		}

		public static AsWeakInstanceOfConditional Create (BasicBlockScanner scanner, ref BasicBlock bb, ref int index, TypeDefinition type)
		{
			if (bb.Instructions.Count < 2)
				throw new NotSupportedException ();
			if (index + 1 >= scanner.Body.Instructions.Count)
				throw new NotSupportedException ();

			/*
			 * `bool MonoLinkerSupport.AsWeakInstance<T> (object obj, out T instance)`
			 */

			var load = scanner.Body.Instructions [index - 2];
			var output = scanner.Body.Instructions [index - 1];

			scanner.LogDebug (1, $"WEAK INSTANCE OF: {bb} {index} {type} - {load} {output}");

			if (!CecilHelper.IsSimpleLoad (load))
				throw new NotSupportedException ();
			if (output.OpCode.Code != Code.Ldloca && output.OpCode.Code != Code.Ldloca_S)
				throw new NotSupportedException ();

			if (bb.Instructions.Count > 3)
				scanner.BlockList.SplitBlockAt (ref bb, bb.Instructions.Count - 2);
			var instanceType = CecilHelper.GetWeakInstanceArgument (bb.Instructions [2]);
			var variable = ((VariableReference)output.Operand).Resolve () ?? throw new NotSupportedException ();

			var instance = new AsWeakInstanceOfConditional (scanner, instanceType, variable);
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
			return $"[{GetType ().Name}: {InstanceType.Name} {Variable}]";
		}
	}
}
