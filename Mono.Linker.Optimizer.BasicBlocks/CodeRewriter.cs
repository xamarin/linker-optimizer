//
// CodeRewriter.cs
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
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Mono.Linker.Optimizer.BasicBlocks
{
	public class CodeRewriter
	{
		public BasicBlockScanner Scanner {
			get;
		}

		public OptimizerContext Context => Scanner.Context;

		public BasicBlockList BlockList => Scanner.BlockList;

		public MethodDefinition Method => Scanner.Method;

		public MethodBody Body => Scanner.Body;

		public CodeRewriter (BasicBlockScanner scanner)
		{
			Scanner = scanner;
		}

		/*
		 * The block @block contains a linker conditional that resolves into the
		 * boolean constant @condition.  There are @stackDepth extra values on the
		 * stack and the block ends with a branch instruction.
		 *
		 * If @condition is true, then we replace the branch with a direct jump.
		 *
		 * If @condition is false, then we remove the branch.
		 *
		 * In either case, we need to make sure to pop the extra @stackDepth values
		 * off the stack.
		 *
		 */
		public void ReplaceWithBranch (ref BasicBlock block, int stackDepth, bool condition)
		{
			if (!CecilHelper.IsBranch (block.BranchType))
				throw new OptimizerAssertionException ($"{nameof (ReplaceWithBranch)} used on non-branch block.");

			Instruction branch = null;
			if (condition)
				branch = Instruction.Create (OpCodes.Br, (Instruction)block.LastInstruction.Operand);

			ReplaceWithInstruction (ref block, stackDepth, branch);
		}

		/*
		 * Replace block @block with a constant @constant, optionally popping
		 * @stackDepth extra values off the stack.
		 */
		public void ReplaceWithConstant (ref BasicBlock block, int stackDepth, ConstantValue constant)
		{
			Instruction instruction;
			switch (constant) {
			case ConstantValue.False:
				instruction = Instruction.Create (OpCodes.Ldc_I4_0);
				break;
			case ConstantValue.True:
				instruction = Instruction.Create (OpCodes.Ldc_I4_1);
				break;
			case ConstantValue.Null:
				instruction = Instruction.Create (OpCodes.Ldnull);
				break;
			default:
				throw DebugHelpers.AssertFailUnexpected (Method, block, constant);

			}

			switch (block.BranchType) {
			case BranchType.None:
				ReplaceWithInstruction (ref block, stackDepth, instruction);
				break;
			case BranchType.Return:
				// Rewrite as constant, then put back the return
				Scanner.Rewriter.ReplaceWithInstruction (ref block, stackDepth, instruction, false);
				BlockList.InsertInstructionAt (ref block, block.Count, Instruction.Create (OpCodes.Ret));
				BlockList.TryMergeBlock (ref block);
				break;
			default:
				throw new OptimizerAssertionException ($"{nameof (ReplaceWithConstant)} called on unsupported block type `{block.BranchType}`.");
			}
		}

		/*
		 * Replace block @block with an `isinst`, but keep @index instructions at the beginning
		 * of the block and the (optional) branch at the end.
		 *
		 * The block is expected to contain the following:
		 * 
		 * - optional simple load instruction
		 * - conditional call
		 * - optional branch instruction
		 *
		 */
		public void ReplaceWithIsInst (ref BasicBlock block, int index, TypeDefinition type)
		{
			if (index < 0 || index >= block.Count)
				throw new ArgumentOutOfRangeException (nameof (index));

			if (block.Instructions [index].OpCode.Code != Code.Call)
				DebugHelpers.AssertFailUnexpected (Method, block, block.Instructions [index]);

			/*
			 * The block consists of the following:
			 *
			 * - optional simple load instruction
			 * - conditional call
			 * - optional branch instruction
			 *
			 */

			var reference = Method.DeclaringType.Module.ImportReference (type);

			BlockList.ReplaceInstructionAt (ref block, index++, Instruction.Create (OpCodes.Isinst, reference));

			switch (block.BranchType) {
			case BranchType.False:
			case BranchType.True:
				DebugHelpers.Assert (index == block.Count - 1);
				break;
			case BranchType.None:
				// Convert it into a bool.
				DebugHelpers.Assert (index == block.Count);
				BlockList.InsertInstructionAt (ref block, index++, Instruction.Create (OpCodes.Ldnull));
				BlockList.InsertInstructionAt (ref block, index++, Instruction.Create (OpCodes.Cgt_Un));
				break;
			case BranchType.Return:
				// Convert it into a bool.
				DebugHelpers.Assert (index == block.Count - 1);
				BlockList.InsertInstructionAt (ref block, index++, Instruction.Create (OpCodes.Ldnull));
				BlockList.InsertInstructionAt (ref block, index++, Instruction.Create (OpCodes.Cgt_Un));
				break;
			default:
				throw DebugHelpers.AssertFailUnexpected (Method, block, block.BranchType);
			}

			BlockList.TryMergeBlock (ref block);
		}

		/*
		 * Replace block @block with a `throw new PlatformNotSupportedException ()`.
		 *
		 */
		public void ReplaceWithThrow (ref BasicBlock block, int stackDepth)
		{
			if (block.LastInstruction.OpCode.Code != Code.Call && block.LastInstruction.OpCode.Code != Code.Callvirt)
				DebugHelpers.AssertFailUnexpected (Method, block, block.LastInstruction);

			var list = new List<Instruction> {
				Context.CreateNewPlatformNotSupportedException (Method),
				Instruction.Create (OpCodes.Throw)
			};

			ReplaceWithInstructions (ref block, stackDepth, list);
		}


		/*
		 * Replace the entire block with the following:
		 *
		 * - pop @stackDepth values from the stack
		 * - (optional) instruction @instruction
		 *
		 * The block will be deleted if this would result in an empty block.
		 *
		 */
		void ReplaceWithInstruction (ref BasicBlock block, int stackDepth, Instruction instruction, bool merge = true)
		{
			Scanner.LogDebug (1, $"REPLACE INSTRUCTION: {block} {stackDepth} {instruction}");
			Scanner.DumpBlock (1, block);

			// Remove everything except the first instruction.
			while (block.Count > 1)
				BlockList.RemoveInstructionAt (ref block, 1);

			if (stackDepth == 0) {
				if (instruction == null)
					BlockList.RemoveInstructionAt (ref block, 0);
				else
					BlockList.ReplaceInstructionAt (ref block, 0, instruction);
				if (merge && block != null)
					BlockList.TryMergeBlock (ref block);
				return;
			}

			BlockList.ReplaceInstructionAt (ref block, 0, Instruction.Create (OpCodes.Pop));
			for (int i = 1; i < stackDepth; i++)
				BlockList.InsertInstructionAt (ref block, i, Instruction.Create (OpCodes.Pop));

			if (instruction != null)
				BlockList.InsertInstructionAt (ref block, stackDepth, instruction);

			if (merge)
				BlockList.TryMergeBlock (ref block);
		}

		/*
		 * Replace the entire block with the following:
		 *
		 * - pop @stackDepth values from the stack
		 * - (optional) instructions @instructions
		 *
		 * The block will be deleted if this would result in an empty block.
		 *
		 */
		void ReplaceWithInstructions (ref BasicBlock block, int stackDepth, List<Instruction> instructions, bool merge = false)
		{
			for (int i = 0; i < stackDepth; i++)
				instructions.Insert (0, Instruction.Create (OpCodes.Pop));
			ReplaceWithInstructions (ref block, instructions, merge);
		}

		void ReplaceWithInstructions (ref BasicBlock block, List<Instruction> instructions, bool merge = false)
		{
			Scanner.LogDebug (1, $"REPLACE INSTRUCTION: {block}");
			Scanner.DumpBlock (1, block);

			// Remove everything except the first instruction.
			while (block.Count > 1)
				BlockList.RemoveInstructionAt (ref block, 1);

			BlockList.ReplaceInstructionAt (ref block, 0, instructions [0]);
			for (int index = 1; index < instructions.Count; index++)
				BlockList.InsertInstructionAt (ref block, index, instructions [index]);

			if (merge)
				BlockList.TryMergeBlock (ref block);
		}

		/*
		 * Replace the entire method body with `throw new PlatformNotSupportedException ()`.
		 */
		public static void ReplaceWithPlatformNotSupportedException (OptimizerContext context, MethodDefinition method)
		{
			if (method.IsAbstract || method.IsVirtual || method.IsConstructor || !method.IsIL)
				throw ThrowUnsupported (method, "Cannot rewrite method of this type into throwing an exception.");
			if (method.HasParameters)
				throw ThrowUnsupported (method, "Can only rewrite parameterless methods into throwing an exception.");
			method.Body.Instructions.Clear ();
			method.Body.Instructions.Add (context.CreateNewPlatformNotSupportedException (method));
			method.Body.Instructions.Add (Instruction.Create (OpCodes.Throw));
			context.MarkAsConstantMethod (method, ConstantValue.Throw);
		}

		/*
		 * Replace the entire method body with `return null`.
		 */
		public static void ReplaceWithReturnNull (OptimizerContext context, MethodDefinition method)
		{
			if (method.IsAbstract || method.IsVirtual || method.IsConstructor || !method.IsIL)
				throw ThrowUnsupported (method, "Cannot rewrite method of this type into returning null.");
			if (method.HasParameters)
				throw ThrowUnsupported (method, "Can only rewrite parameterless methods into returning null.");

			if (method.ReturnType.MetadataType != MetadataType.Class && method.ReturnType.MetadataType != MetadataType.Object)
				throw ThrowUnsupported (method, "Can only rewrite methods returning class or object into returning null.");

			method.Body.Instructions.Clear ();
			method.Body.Instructions.Add (Instruction.Create (OpCodes.Ldnull));
			method.Body.Instructions.Add (Instruction.Create (OpCodes.Ret));
			context.MarkAsConstantMethod (method, ConstantValue.Null);
		}

		/*
		 * Replace the entire method body with `return false`.
		 */
		public static void ReplaceWithReturnFalse (OptimizerContext context, MethodDefinition method)
		{
			if (method.IsAbstract || method.IsVirtual || method.IsConstructor || !method.IsIL)
				throw ThrowUnsupported (method, "Cannot rewrite method of this type into returning false.");
			if (method.HasParameters)
				throw ThrowUnsupported (method, "Can only rewrite parameterless methods into returning false.");

			if (method.ReturnType.MetadataType != MetadataType.Boolean)
				throw ThrowUnsupported (method, "Can only rewrite methods returning bool into returning false.");

			method.Body.Instructions.Clear ();
			method.Body.Instructions.Add (Instruction.Create (OpCodes.Ldc_I4_0));
			method.Body.Instructions.Add (Instruction.Create (OpCodes.Ret));
			context.MarkAsConstantMethod (method, ConstantValue.False);
		}

		/*
		 * Replace the entire method body with `return true`.
		 */
		public static void ReplaceWithReturnTrue (OptimizerContext context, MethodDefinition method)
		{
			if (method.IsAbstract || method.IsVirtual || method.IsConstructor || !method.IsIL)
				throw ThrowUnsupported (method, "Cannot rewrite method of this type into returning true.");
			if (method.HasParameters)
				throw ThrowUnsupported (method, "Can only rewrite parameterless methods into returning true.");

			if (method.ReturnType.MetadataType != MetadataType.Boolean)
				throw ThrowUnsupported (method, "Can only rewrite methods returning bool into returning true.");

			method.Body.Instructions.Clear ();
			method.Body.Instructions.Add (Instruction.Create (OpCodes.Ldc_I4_1));
			method.Body.Instructions.Add (Instruction.Create (OpCodes.Ret));
			context.MarkAsConstantMethod (method, ConstantValue.True);
		}

		static Exception ThrowUnsupported (MethodDefinition method, string message)
		{
			throw new OptimizerAssertionException ($"Code refactoring error in `{method}`: {message}");
		}
	}
}
