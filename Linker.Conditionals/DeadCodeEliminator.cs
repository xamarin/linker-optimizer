//
// DeadCodeEliminator.cs
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
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Mono.Linker.Conditionals
{
	public class DeadCodeEliminator
	{
		public BasicBlockScanner Scanner {
			get;
		}

		public BasicBlockList BlockList => Scanner.BlockList;

		protected MethodDefinition Method => BlockList.Body.Method;

		public DeadCodeEliminator (BasicBlockScanner scanner)
		{
			Scanner = scanner;
		}

		public bool RemoveDeadBlocks ()
		{
			Scanner.LogDebug (2, $"REMOVE DEAD BLOCKS: {Method.Name}");
			Scanner.DumpBlocks (2);

			var foundDeadBlocks = false;
			for (int i = 0; i < BlockList.Count; i++) {
				var block = BlockList [i];
				if (!block.IsDead)
					continue;

				Scanner.LogDebug (2, $"  FOUND DEAD BLOCK: {block}");

				if (CecilHelper.IsBranch (block.BranchType)) {
					var target = BlockList.GetBlock ((Instruction)block.LastInstruction.Operand);
					target.RemoveJumpOrigin (block.LastInstruction);
				}

				if (block.ExceptionHandlers.Count > 0)
					CheckRemoveExceptionBlock (ref i);

				foundDeadBlocks = true;
			}

			Scanner.LogDebug (2, $"REMOVE DEAD BLOCKS #1: {Method.Name} {foundDeadBlocks}");

			if (!foundDeadBlocks)
				return false;

			var removedDeadBlocks = false;
			for (int i = 0; i < BlockList.Count; i++) {
				var block = BlockList [i];
				if (!block.IsDead)
					continue;

				Scanner.LogDebug (2, $"  DELETING DEAD BLOCK: {block}");

				BlockList.DeleteBlock (ref block);
				removedDeadBlocks = true;
				i--;
			}

			if (removedDeadBlocks) {
				BlockList.ComputeOffsets ();

				Scanner.LogDebug (2, $"REMOVE DEAD BLOCKS DONE: {Method.Name} {foundDeadBlocks}");
				Scanner.DumpBlocks (2);
			}

			return removedDeadBlocks;
		}

		void CheckRemoveExceptionBlock (ref int index)
		{
			var block = BlockList [index];
			int end_index = index + 1;

			Scanner.LogDebug (2, $"  CHECKING DEAD EXCEPTION BLOCK: {block}");

			while (block.ExceptionHandlers.Count > 0) {
				var handler = block.ExceptionHandlers [0];
				var handler_index = BlockList.IndexOf (BlockList.GetBlock (handler.HandlerEnd));
				if (handler_index > end_index)
					end_index = handler_index;

				block.ExceptionHandlers.RemoveAt (0);
				BlockList.Body.ExceptionHandlers.Remove (handler);

				block.GetJumpOrigins ().RemoveAll (j => j.Exception == handler);
			}

			Scanner.LogDebug (2, $"  CHECKING DEAD EXCEPTION BLOCK: {block} {end_index}");

			while (end_index > index) {
				var current = BlockList [index];
				if (!current.IsDead)
					throw new NotSupportedException ();

				Scanner.LogDebug (2, $"      MARKING DEAD: {current}");

				end_index--;
			}

			index = -1;
		}

		public bool RemoveDeadJumps ()
		{
			Scanner.LogDebug (2, $"REMOVE DEAD JUMPS: {Method.Name}");
			Scanner.DumpBlocks (2);

			var removedDeadBlocks = false;

			for (int i = 0; i < BlockList.Count - 1; i++) {
				if (BlockList [i].BranchType != BranchType.Jump)
					continue;

				var lastInstruction = BlockList [i].LastInstruction;
				var nextInstruction = BlockList [i + 1].FirstInstruction;
				if (lastInstruction.OpCode.Code != Code.Br && lastInstruction.OpCode.Code != Code.Br_S)
					continue;
				if ((Instruction)lastInstruction.Operand != nextInstruction)
					continue;

				Scanner.LogDebug (2, $"ELIMINATE DEAD JUMP: {lastInstruction}");

				removedDeadBlocks = true;

				BlockList [i + 1].RemoveJumpOrigin (lastInstruction);

				var block = BlockList [i];
				BlockList.RemoveInstructionAt (ref block, block.Count - 1);
				if (block != null)
					BlockList.TryMergeBlock (ref block);
			}

			if (removedDeadBlocks) {
				BlockList.ComputeOffsets ();

				Scanner.LogDebug (2, $"REMOVE DEAD JUMPS DONE: {Method.Name}");
				Scanner.DumpBlocks (2);
			}

			return removedDeadBlocks;
		}

		public bool RemoveConstantJumps ()
		{
			var removedConstantJumps = false;

			Scanner.LogDebug (2, $"REMOVE CONSTANT BLOCKS: {Method.Name}");
			Scanner.DumpBlocks (2);

			for (int i = 0; i < BlockList.Count - 1; i++) {
				var block = BlockList [i];
				removedConstantJumps |= RemoveConstantJumps (ref block, i);
			}

			if (removedConstantJumps) {
				BlockList.ComputeOffsets ();

				Scanner.LogDebug (2, $"REMOVE CONSTANT JUMPS DONE: {Method.Name}");
				Scanner.DumpBlocks (2);
			}

			return removedConstantJumps;
		}

		bool RemoveConstantJumps (ref BasicBlock block, int index)
		{
			if (Scanner.DebugLevel > 2) {
				Scanner.LogDebug (2, $"  #{index}: {block}");
				Scanner.LogDebug (2, "  ", null, block.JumpOrigins);
				Scanner.LogDebug (2, "  ", null, block.Instructions);
				Scanner.Context.Debug ();
			}

			bool constant;
			switch (block.BranchType) {
			case BranchType.True:
				if (block.Count < 2)
					return false;
				if (!CecilHelper.IsConstantLoad (block.Instructions [block.Count - 2], out constant))
					return false;
				break;
			case BranchType.False:
				if (block.Count < 2)
					return false;
				if (!CecilHelper.IsConstantLoad (block.Instructions [block.Count - 2], out constant))
					return false;
				constant = !constant;
				break;
			default:
				return false;
			}

			var target = (Instruction)block.LastInstruction.Operand;

			Scanner.LogDebug (2, $"  ELIMINATE CONSTANT JUMP: {constant} {block.LastInstruction} {CecilHelper.Format (target)}");

			BlockList.RemoveInstructionAt (ref block, block.Count - 1);
			if (constant)
				BlockList.ReplaceInstructionAt (ref block, block.Count - 1, Instruction.Create (OpCodes.Br, target));
			else
				BlockList.RemoveInstructionAt (ref block, block.Count - 1);

			return true;
		}

		public bool RemoveUnusedVariables ()
		{
			Scanner.LogDebug (1, $"REMOVE VARIABLES: {Method.Name}");

			if (Method.Body.HasExceptionHandlers)
				return false;

			var removed = false;
			var variables = new Dictionary<VariableDefinition, VariableEntry> ();

			for (int i = 0; i < Method.Body.Variables.Count; i++) {
				var variable = new VariableEntry (Method.Body.Variables [i], i);
				variables.Add (variable.Variable, variable);
			}

			foreach (var block in BlockList.Blocks) {
				Scanner.LogDebug (2, $"REMOVE VARIABLES #1: {block}");

				for (int i = 0; i < block.Instructions.Count; i++) {
					var instruction = block.Instructions [i];
					Scanner.LogDebug (2, $"    {CecilHelper.Format (instruction)}");

					var variable = CecilHelper.GetVariable (Method.Body, instruction);
					if (variable == null)
						continue;

					var entry = variables [variable];
					if (entry == null)
						throw DebugHelpers.AssertFail (Method, block, $"Cannot resolve variable from instruction `{CecilHelper.Format (instruction)}`.");

					entry.Used = true;
					if (entry.Modified)
						continue;

					switch (instruction.OpCode.Code) {
					case Code.Ldloc_0:
					case Code.Ldloc_1:
					case Code.Ldloc_2:
					case Code.Ldloc_3:
					case Code.Ldloc:
					case Code.Ldloc_S:
						continue;

					case Code.Ldloca:
					case Code.Ldloca_S:
						entry.SetModified ();
						continue;

					case Code.Stloc:
					case Code.Stloc_0:
					case Code.Stloc_1:
					case Code.Stloc_2:
					case Code.Stloc_3:
					case Code.Stloc_S:
						break;

					default:
						throw DebugHelpers.AssertFailUnexpected (Method, block, instruction);
					}

					if (i == 0 || entry.IsConstant) {
						entry.SetModified ();
						continue;
					}

					var load = block.Instructions [i - 1];
					switch (block.Instructions [i - 1].OpCode.Code) {
					case Code.Ldc_I4_0:
						entry.SetConstant (block, load, 0);
						break;
					case Code.Ldc_I4_1:
						entry.SetConstant (block, load, 1);
						break;
					default:
						entry.SetModified ();
						break;
					}
				}
			}

			Scanner.LogDebug (1, $"REMOVE VARIABLES #1");

			for (int i = Method.Body.Variables.Count - 1; i >= 0; i--) {
				var variable = variables [Method.Body.Variables [i]];
				Scanner.LogDebug (2, $"    VARIABLE #{i}: {variable}");
				if (!variable.Used) {
					Scanner.LogDebug (2, $"    --> REMOVE");
					Scanner.LogDebug (1, $"REMOVE VARIABLES - REMOVE: {Method.Name}");
					RemoveVariable (variable);
					Method.Body.Variables.RemoveAt (i);
					removed = true;
					continue;
				}

				if (variable.IsConstant) {
					Scanner.LogDebug (2, $"    --> CONSTANT ({variable.Value}): {variable.Instruction}");
					Scanner.LogDebug (1, $"REMOVE VARIABLES - CONSTANT: {Method.Name}");
					Scanner.DumpBlock (2, variable.Block);
					var position = variable.Block.IndexOf (variable.Instruction);
					var block = variable.Block;
					BlockList.RemoveInstructionAt (ref block, position + 1);
					BlockList.RemoveInstructionAt (ref block, position);
					RemoveVariable (variable);
					Method.Body.Variables.RemoveAt (i);
					removed = true;
					continue;
				}
			}

			if (removed) {
				BlockList.ComputeOffsets ();

				Scanner.LogDebug (1, $"REMOVE VARIABLES DONE: {removed}");
				Scanner.DumpBlocks (1);
			}

			return removed;
		}

		void RemoveVariable (VariableEntry variable)
		{
			if (Scanner.DebugLevel > 1) {
				BlockList.ComputeOffsets ();
				Scanner.LogDebug (2, $"DELETE VARIABLE: {Method.Name} {variable}");
				Scanner.DumpBlocks (2);

			}

			for (int i = 0; i < BlockList.Count; i++) {
				var block = BlockList [i];
				for (int j = 0; j < block.Instructions.Count; j++) {
					var instruction = block.Instructions [j];
					Scanner.LogDebug (2, $"    {CecilHelper.Format (instruction)}");

					switch (instruction.OpCode.Code) {
					case Code.Ldloc_0:
						if (variable.Index == 0 && variable.IsConstant)
							BlockList.ReplaceInstructionAt (ref block, j, CecilHelper.CreateConstantLoad (variable.Value));
						break;
					case Code.Stloc_0:
						break;
					case Code.Ldloc_1:
						if (variable.Index < 1)
							BlockList.ReplaceInstructionAt (ref block, j, Instruction.Create (OpCodes.Ldloc_0));
						else if (variable.Index == 1)
							BlockList.ReplaceInstructionAt (ref block, j, CecilHelper.CreateConstantLoad (variable.Value));
						break;
					case Code.Stloc_1:
						if (variable.Index < 1)
							BlockList.ReplaceInstructionAt (ref block, j, Instruction.Create (OpCodes.Stloc_0));
						break;
					case Code.Ldloc_2:
						if (variable.Index < 2)
							BlockList.ReplaceInstructionAt (ref block, j, Instruction.Create (OpCodes.Ldloc_1));
						else if (variable.Index == 2)
							BlockList.ReplaceInstructionAt (ref block, j, CecilHelper.CreateConstantLoad (variable.Value));
						break;
					case Code.Stloc_2:
						if (variable.Index < 2)
							BlockList.ReplaceInstructionAt (ref block, j, Instruction.Create (OpCodes.Stloc_1));
						break;
					case Code.Ldloc_3:
						if (variable.Index < 3)
							BlockList.ReplaceInstructionAt (ref block, j, Instruction.Create (OpCodes.Ldloc_2));
						else if (variable.Index == 3)
							BlockList.ReplaceInstructionAt (ref block, j, CecilHelper.CreateConstantLoad (variable.Value));
						break;
					case Code.Stloc_3:
						if (variable.Index < 3)
							BlockList.ReplaceInstructionAt (ref block, j, Instruction.Create (OpCodes.Stloc_2));
						break;
					case Code.Ldloc:
					case Code.Ldloc_S:
						var argument = CecilHelper.GetVariable (Method.Body, instruction);
						if (argument == variable.Variable)
							BlockList.ReplaceInstructionAt (ref block, j, CecilHelper.CreateConstantLoad (variable.Value));
						break;
					}
				}
			}

			if (Scanner.DebugLevel > 1) {
				BlockList.ComputeOffsets ();
				Scanner.LogDebug (2, $"DELETE VARIABLE DONE: {Method.Name} {variable}");
				Scanner.DumpBlocks (2);
			}
		}

		class VariableEntry
		{
			public VariableDefinition Variable {
				get;
			}

			public int Index {
				get;
			}

			public bool Used {
				get; set;
			}

			public bool Modified {
				get;
				private set;
			}

			public bool IsConstant => Block != null;

			public BasicBlock Block {
				get;
				private set;
			}

			public Instruction Instruction {
				get;
				private set;
			}

			public int Value {
				get;
				private set;
			}

			public VariableEntry (VariableDefinition variable, int index)
			{
				Variable = variable;
				Index = index;
			}

			public void SetModified ()
			{
				Modified = true;
				Block = null;
				Instruction = null;
				Value = 0;
			}

			public void SetConstant (BasicBlock block, Instruction instruction, int value)
			{
				if (Modified)
					throw new InvalidOperationException ();
				Block = block;
				Instruction = instruction;
				Value = value;
			}

			public override string ToString ()
			{
				return $"[{Variable}: used={Used}, modified={Modified}, constant={IsConstant}]";
			}
		}
	}
}
