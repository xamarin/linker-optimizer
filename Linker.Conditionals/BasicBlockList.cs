//
// BasicBlockList.cs
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

namespace Mono.Linker.Conditionals
{
	public class BasicBlockList
	{
		public BasicBlockScanner Scanner {
			get;
		}

		public MethodBody Body {
			get;
		}

		public MethodDefinition Method => Body.Method; 

		readonly Dictionary<Instruction, BasicBlock> _bb_by_instruction;
		readonly List<BasicBlock> _block_list;
		int _next_block_id;

		public IReadOnlyList<BasicBlock> Blocks => _block_list;

		public BasicBlockList (BasicBlockScanner scanner, MethodBody body)
		{
			Scanner = scanner;
			Body = body;

			_bb_by_instruction = new Dictionary<Instruction, BasicBlock> ();
			_block_list = new List<BasicBlock> ();
		}

		public BasicBlock NewBlock (Instruction instruction)
		{
			var block = new BasicBlock (++_next_block_id, instruction);
			_bb_by_instruction.Add (instruction, block);
			_block_list.Add (block);
			return block;
		}

		internal void ReplaceBlock (ref BasicBlock block, IList<Instruction> instructions)
		{
			if (instructions.Count < 1)
				throw new ArgumentOutOfRangeException ();

			var oldBlock = block;
			var blockIndex = _block_list.IndexOf (block);
			var oldInstruction = block.Instructions [0];
			oldBlock.Type = BasicBlockType.Deleted;
			oldInstruction.Offset = -1;

			_bb_by_instruction.Remove (oldInstruction);

			CheckRemoveJumpOrigin (oldBlock);

			block = new BasicBlock (++_next_block_id, instructions);
			_block_list [blockIndex] = block;
			_bb_by_instruction.Add (instructions [0], block);

			AdjustJumpTargets (oldBlock, block);

			CheckAddJumpOrigin (block);
		}

		void AdjustJumpTargets (BasicBlock oldBlock, BasicBlock newBlock)
		{
			foreach (var origin in oldBlock.JumpOrigins) {
				if (newBlock == null)
					throw CannotRemoveTarget;
				if (origin.Exception != null) {
					AdjustException (origin.Exception);
					newBlock.AddJumpOrigin (origin);
				} else {
					newBlock.AddJumpOrigin (new JumpOrigin (newBlock, origin.OriginBlock, origin.Origin));
					AdjustJump (origin);
				}
			}

			var oldInstruction = oldBlock.LastInstruction;

			Scanner.LogDebug (2, $"ADJUST JUMPS: {oldBlock} {newBlock} {oldInstruction}");
			Scanner.Context.Debug ();

			foreach (var block in _block_list) {
				for (var j = 0; j < block.JumpOrigins.Count; j++) {
					var origin = block.JumpOrigins [j];
					Scanner.LogDebug (2, $"  ORIGIN: {origin}");
					if (origin.Exception != null) {
						Scanner.LogDebug (2, $"  EXCEPTION ORIGIN: {origin}");
						if (origin.Exception.TryStart == oldInstruction)
							throw CannotRemoveTarget;
						if (origin.Exception.HandlerStart == oldInstruction)
							throw CannotRemoveTarget;
					}
					if (origin.Origin == oldInstruction)
						throw CannotRemoveTarget;
					if (origin.OriginBlock == oldBlock) {
						origin.OriginBlock = newBlock ?? throw CannotRemoveTarget;
						continue;
					}
				}
			}

			void AdjustException (ExceptionHandler handler)
			{
				if (handler.TryStart == oldBlock.FirstInstruction)
					handler.TryStart = newBlock.FirstInstruction;
				if (handler.TryEnd == oldBlock.FirstInstruction)
					handler.TryEnd = newBlock.FirstInstruction;
				if (handler.HandlerStart == oldBlock.FirstInstruction)
					handler.HandlerStart = newBlock.FirstInstruction;
				if (handler.HandlerEnd == oldBlock.FirstInstruction)
					handler.HandlerEnd = newBlock.FirstInstruction;
				if (handler.FilterStart == oldBlock.FirstInstruction)
					handler.FilterStart = newBlock.FirstInstruction;
			}

			void AdjustJump (JumpOrigin origin)
			{
				var instruction = origin.Origin;
				if (instruction.OpCode.OperandType == OperandType.InlineSwitch) {
					var labels = (Instruction [])instruction.Operand;
					for (int i = 0; i < labels.Length; i++) {
						if (labels [i] != oldBlock.FirstInstruction)
							continue;
						labels [i] = newBlock?.FirstInstruction ?? throw CannotRemoveTarget;
					}
					return;
				}
				if (instruction.OpCode.OperandType != OperandType.InlineBrTarget &&
				    instruction.OpCode.OperandType != OperandType.ShortInlineBrTarget)
					throw DebugHelpers.AssertFailUnexpected (Method, origin.OriginBlock, instruction);
				if (instruction.Operand != oldBlock.FirstInstruction)
					throw DebugHelpers.AssertFailUnexpected (Method, origin.OriginBlock, instruction);
				instruction.Operand = newBlock?.FirstInstruction ?? throw CannotRemoveTarget;
			}
		}

		internal void CheckRemoveJumpOrigin (BasicBlock block)
		{
			if (CecilHelper.IsBranch (block.BranchType)) {
				var target = GetBlock ((Instruction)block.LastInstruction.Operand);
				target.RemoveJumpOrigin (block.LastInstruction);
			}
		}

		void CheckAddJumpOrigin (BasicBlock block)
		{
			if (CecilHelper.IsBranch (block.BranchType)) {
				// We are adding a new branch instruction.
				var target = _bb_by_instruction [(Instruction)block.LastInstruction.Operand];
				target.AddJumpOrigin (new JumpOrigin (target, block, block.LastInstruction));
			}
		}

		public bool Initialize ()
		{
			_bb_by_instruction.Clear ();
			_block_list.Clear ();
			_next_block_id = 0;

			foreach (var handler in Body.ExceptionHandlers) {
				if (handler.TryStart != null)
					EnsureExceptionBlock (BasicBlockType.Try, handler.TryStart, handler);
				switch (handler.HandlerType) {
				case ExceptionHandlerType.Catch:
					EnsureExceptionBlock (BasicBlockType.Catch, handler.HandlerStart, handler);
					EnsureExceptionBlock (BasicBlockType.Normal, handler.HandlerEnd, handler);
					break;
				case ExceptionHandlerType.Finally:
					EnsureExceptionBlock (BasicBlockType.Finally, handler.HandlerStart, handler);
					EnsureExceptionBlock (BasicBlockType.Normal, handler.HandlerEnd, handler);
					break;
				default:
					Scanner.LogDebug (1, $"Unknown exception type `{handler.HandlerType}` in `{Scanner.Method}`.");
					return false;
				}
			}

			foreach (var instruction in Body.Instructions) {
				switch (instruction.OpCode.OperandType) {
				case OperandType.InlineBrTarget:
				case OperandType.ShortInlineBrTarget:
					EnsureBlock ((Instruction)instruction.Operand);
					break;
				case OperandType.InlineSwitch:
					foreach (var label in (Instruction [])instruction.Operand)
						EnsureBlock (label);
					break;
				}
			}

			return true;

			void EnsureExceptionBlock (BasicBlockType type, Instruction target, ExceptionHandler handler)
			{
				if (target == null)
					return;
				var block = EnsureBlock (target);
				if (block.Type == BasicBlockType.Normal)
					block.Type = type;
				else if (block.Type != type)
					throw DebugHelpers.AssertFailUnexpected (Method, block, type);
				block.ExceptionHandlers.Add (handler);
				block.AddJumpOrigin (new JumpOrigin (block, handler));
			}
		}

		BasicBlock EnsureBlock (Instruction target)
		{
			if (_bb_by_instruction.TryGetValue (target, out var block))
				return block;
			block = new BasicBlock (++_next_block_id, BasicBlockType.Normal, target);
			_bb_by_instruction.Add (target, block);
			_block_list.Add (block);
			return block;
		}

		internal void AddJumpOrigin (BasicBlock current, Instruction origin, Instruction target)
		{
			var block = _bb_by_instruction [target];
			block.AddJumpOrigin (new JumpOrigin (block, current, origin));
		}

		Exception CannotRemoveTarget => throw new NotSupportedException ("Attempted to remove a basic block that's being jumped to.");

		public bool SplitBlockAt (ref BasicBlock block, int position)
		{
			if (block.Instructions.Count < position)
				throw new ArgumentOutOfRangeException ();
			if (block.Instructions.Count == position)
				return false;

			var blockIndex = _block_list.IndexOf (block);

			block.Type = BasicBlockType.Deleted;

			var previousInstructions = block.GetInstructions (0, position);
			var nextInstructions = block.GetInstructions (position);

			var previousBlock = new BasicBlock (++_next_block_id, previousInstructions);
			_block_list [blockIndex] = previousBlock;
			_bb_by_instruction [previousInstructions [0]] = previousBlock;

			AdjustJumpTargets (block, previousBlock);

			block = new BasicBlock (++_next_block_id, nextInstructions);
			_block_list.Insert (blockIndex + 1, block);
			_bb_by_instruction.Add (nextInstructions [0], block);
			return true;
		}

		public int Count => _block_list.Count;

		public BasicBlock this [int index] => _block_list [index];

		public BasicBlock GetBlock (Instruction instruction) => _bb_by_instruction [instruction];

		public bool TryGetBlock (Instruction instruction, out BasicBlock block)
		{
			return _bb_by_instruction.TryGetValue (instruction, out block);
		}

		public bool HasBlock (Instruction instruction) => _bb_by_instruction.ContainsKey (instruction);

		public int IndexOf (BasicBlock block) => _block_list.IndexOf (block);

		public void ComputeOffsets ()
		{
			var offset = 0;
			foreach (var instruction in Body.Instructions) {
				instruction.Offset = offset;
				offset += instruction.GetSize ();
			}

			_block_list.Sort ((first, second) => first.FirstInstruction.Offset.CompareTo (second.FirstInstruction.Offset));

			for (int i = 0; i < _block_list.Count; i++)
				_block_list [i].Index = i;
		}

		public void Dump ()
		{
			Scanner.Context.LogMessage (MessageImportance.Low, $"BLOCK DUMP ({Body.Method})");
			foreach (var block in _block_list) {
				Dump (block);
			}
		}

		public void Dump (BasicBlock block)
		{
			Scanner.Context.LogMessage (MessageImportance.Low, $"{block}:");
			Scanner.LogDebug (0, "  ", null, block.JumpOrigins);
			Scanner.LogDebug (0, "  ", null, block.Instructions);
		}

		public void RemoveInstructionAt (ref BasicBlock block, int position)
		{
			if (position >= block.Count)
				throw new ArgumentOutOfRangeException (nameof (position));

			var index = _block_list.IndexOf (block);
			if (block.Count == 1) {
				CheckRemoveJumpOrigin (block);
				var next = index < _block_list.Count ? _block_list [index + 1] : null;
				AdjustJumpTargets (block, next);
				DeleteBlock (ref block);
				return;
			}

			if (block.Count < 2)
				throw new InvalidOperationException ("Basic block must have at least one instruction in it.");

			var instruction = block.Instructions [position];
			instruction.Offset = -1;

			Body.Instructions.Remove (instruction);

			// Only the last instruction in a basic block can be a branch.
			if (position == block.Count - 1)
				CheckRemoveJumpOrigin (block);

			if (position > 0) {
				block.RemoveInstructionAt (position);
				return;
			}

			/*
			 * Our logic assumes that the first instruction in a basic block will never change
			 * (because basic blocks are referenced by their first instruction).
			 */

			var instructions = block.Instructions.ToList ();
			instructions.RemoveAt (0);
			ReplaceBlock (ref block, instructions);
		}

		public void InsertInstructionAt (ref BasicBlock block, int position, Instruction instruction)
		{
			if (position < 0 || position > block.Count)
				throw new ArgumentOutOfRangeException (nameof (position));

			int index;
			if (position == block.Count) {
				// Appending to the end.
				index = Body.Instructions.IndexOf (block.LastInstruction);
				Body.Instructions.Insert (index + 1, instruction);
				block.AddInstruction (instruction);
				CheckAddJumpOrigin (block);
				return;
			}

			index = Body.Instructions.IndexOf (block.Instructions [position]);
			Body.Instructions.Insert (index, instruction);

			if (position > 0) {
				block.InsertAt (position, instruction);
				if (position == block.Count - 1)
					CheckAddJumpOrigin (block);
				return;
			}

			/*
			 * Our logic assumes that the first instruction in a basic block will never change
			 * (because basic blocks are referenced by their first instruction).
			 */

			var instructions = block.Instructions.ToList ();
			instructions.Insert (0, instruction);
			ReplaceBlock (ref block, instructions);
		}

		public void ReplaceInstructionAt (ref BasicBlock block, int position, Instruction instruction)
		{
			var old = block.Instructions [position];
			var index = Body.Instructions.IndexOf (old);
			Body.Instructions [index] = instruction;
			instruction.Offset = -1;

			if (position == block.Count - 1)
				CheckRemoveJumpOrigin (block);

			if (position > 0) {
				block.RemoveInstructionAt (position);
				block.InsertAt (position, instruction);
				if (position == block.Count - 1)
					CheckAddJumpOrigin (block);
				return;
			}

			/*
			 * Our logic assumes that the first instruction in a basic block will never change
			 * (because basic blocks are referenced by their first instruction).
			 */

			var instructions = block.Instructions.ToArray ();
			instructions [position] = instruction;

			ReplaceBlock (ref block, instructions);

			if (position == block.Count - 1)
				CheckAddJumpOrigin (block);
		}

		public void DeleteBlock (ref BasicBlock block)
		{
			block.Type = BasicBlockType.Deleted;
			var firstInstruction = block.FirstInstruction;
			var startIndex = Body.Instructions.IndexOf (firstInstruction);
			for (int i = 0; i < block.Count; i++) {
				Body.Instructions [startIndex].Offset = -1;
				Body.Instructions.RemoveAt (startIndex);
			}
			var blockIndex = _block_list.IndexOf (block);
			_block_list.RemoveAt (blockIndex);
			_bb_by_instruction.Remove (firstInstruction);

			block = null;
		}

		bool TryMerge (BasicBlock first, BasicBlock second)
		{
			if (first.LinkerConditional != null || second.LinkerConditional != null)
				return false;
			if (first.BranchType != BranchType.None)
				return false;
			if (second.JumpOrigins.Count > 0)
				return false;

			Scanner.LogDebug (1, $"MERGE BLOCK: {first} {second}");
			Scanner.DumpBlocks (1);
			Scanner.Context.Debug ();

			CheckRemoveJumpOrigin (second);

			var position = Body.Instructions.IndexOf (second.FirstInstruction);

			foreach (var instruction in second.Instructions) {
				Body.Instructions.Insert (position++, instruction);
				first.AddInstruction (instruction);
			}

			CheckAddJumpOrigin (first);

			Scanner.LogDebug (1, $"MERGE BLOCK WITH NEXT #1");
			Scanner.DumpBlocks (1);
			Scanner.Context.Debug ();

			DeleteBlock (ref second);

			Scanner.LogDebug (1, $"MERGE BLOCK WITH NEXT #2");
			Scanner.DumpBlocks (1);
			Scanner.Context.Debug ();

			return true;
		}

		public void TryMergeBlock (ref BasicBlock block)
		{
			if (block == null)
				throw new ArgumentNullException (nameof (block));
			var index = _block_list.IndexOf (block);
			if (index < 0)
				throw new ArgumentOutOfRangeException (nameof (block));

			TryMergeWithNext (ref block, index);
			TryMergeWithPrevious (ref block, index);
		}

		bool TryMergeWithNext (ref BasicBlock block, int index)
		{
			if (index + 1 >= _block_list.Count)
				return false;
			return TryMerge (block, _block_list [index + 1]);
		}

		bool TryMergeWithPrevious (ref BasicBlock block, int index)
		{
			if (index < 1)
				return false;
			if (TryMerge (block, _block_list [index - 1])) {
				block = _block_list [index - 1];
				return true;
			}
			return false;
		}
	}
}
