//
// BasicBlockScanner.cs
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
using System.Diagnostics;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Mono.Linker.Optimizer.BasicBlocks
{
	using Conditionals;

	public class BasicBlockScanner
	{
		public OptimizerContext Context {
			get;
		}

		public MethodDefinition Method {
			get;
		}

		public MethodBody Body => Method.Body;

		public bool FoundConditionals {
			get; private set;
		}

		public BasicBlockList BlockList {
			get;
		}

		public CodeRewriter Rewriter {
			get;
		}

		public int DebugLevel {
			get;
			private set;
		}

		BasicBlockScanner (OptimizerContext context, MethodDefinition method, int? debug = null)
		{
			Context = context;
			Method = method;

			BlockList = new BasicBlockList (this, method.Body);
			Rewriter = new CodeRewriter (this);

			DebugLevel = debug ?? context.GetDebugLevel (method);
		}

		public static bool ThrowOnError;

		public static BasicBlockScanner Scan (OptimizerContext context, MethodDefinition method, int? debug = null)
		{
			var scanner = new BasicBlockScanner (context, method, debug);
			if (!scanner.Scan ())
				return null;
			return scanner;
		}

		public IReadOnlyCollection<BasicBlock> BasicBlocks => BlockList.Blocks;

		[Conditional ("DEBUG")]
		internal void LogDebug (int level, string message)
		{
			if (DebugLevel >= level)
				Context.LogMessage (MessageImportance.Low, message);
		}

		[Conditional ("DEBUG")]
		internal void LogDebug (int level, string indent, string message, IReadOnlyCollection<Instruction> collection)
		{
			LogDebug (level, indent, message, collection, i => CecilHelper.Format (i));
		}

		[Conditional ("DEBUG")]
		internal void LogDebug<T> (int level, string indent, string message, IReadOnlyCollection<T> collection, Func<T, string> formatter = null)
		{
			if (DebugLevel < level || collection.Count == 0)
				return;
			if (!string.IsNullOrEmpty (message))
				Context.LogMessage (MessageImportance.Low, message);
			foreach (var item in collection) {
				var formatted = formatter != null ? formatter (item) : item.ToString ();
				Context.LogMessage (MessageImportance.Low, indent + formatted);
			}
		}

		[Conditional ("DEBUG")]
		internal void DumpBlocks (int level = 1)
		{
			if (DebugLevel >= level)
				BlockList.Dump ();
		}

		[Conditional ("DEBUG")]
		internal void DumpBlock (int level, BasicBlock block)
		{
			if (DebugLevel >= level)
				BlockList.Dump (block);
		}

		bool Scan ()
		{
			LogDebug (1, $"SCAN: {Method}");

			BasicBlock bb = null;

			if (DebugLevel > 0)
				Context.Debug ();

			if (!BlockList.Initialize ())
				return false;

			for (int i = 0; i < Method.Body.Instructions.Count; i++) {
				var instruction = Method.Body.Instructions [i];

				if (BlockList.TryGetBlock (instruction, out var newBB)) {
					if (bb != null && bb.BranchType != BranchType.None)
						throw DebugHelpers.AssertFail (Method, bb, $"Found known basic block with unexpected branch type `{bb.BranchType}`");
					LogDebug (2, $"  KNOWN BB: {newBB}");
					bb = newBB;
				} else if (bb == null) {
					bb = BlockList.NewBlock (instruction);
					LogDebug (2, $"  NEW BB: {bb}");
				} else {
					bb.AddInstruction (instruction);
				}

				var type = CecilHelper.GetBranchType (instruction);
				LogDebug (2, $"    {type}: {CecilHelper.Format (instruction)}");

				if (instruction.OpCode.OperandType == OperandType.InlineMethod) {
					if (LinkerConditional.Scan (this, ref bb, ref i, instruction))
						FoundConditionals = true;
					continue;
				}

				switch (type) {
				case BranchType.None:
					break;

				case BranchType.Conditional:
				case BranchType.False:
				case BranchType.True:
				case BranchType.Jump:
					BlockList.AddJumpOrigin (bb, instruction, (Instruction)instruction.Operand);
					bb = null;
					break;

				case BranchType.Exit:
				case BranchType.Return:
				case BranchType.EndFinally:
					bb = null;
					break;

				case BranchType.Switch:
					foreach (var label in (Instruction [])bb.LastInstruction.Operand)
						BlockList.AddJumpOrigin (bb, instruction, label);
					bb = null;
					break;

				default:
					throw new OptimizerAssertionException ();
				}
			}

			BlockList.ComputeOffsets ();

			DumpBlocks ();

			if (Context.Options.AnalyzeAll || FoundConditionals || DebugLevel > 3) {
				EliminateDeadBlocks ();
				DumpBlocks ();
				return true;
			}

			return true;
		}

		public void RewriteConditionals ()
		{
			LogDebug (1, $"REWRITE CONDITIONALS: {Method.Name}");

			DumpBlocks ();

			var foundConditionals = false;

			foreach (var block in BlockList.Blocks.ToArray ()) {
				if (block.LinkerConditional == null)
					continue;
				RewriteLinkerConditional (block);
				block.LinkerConditional = null;
				foundConditionals = true;
			}

			if (!foundConditionals)
				return;

			BlockList.ComputeOffsets ();

			DumpBlocks ();

			LogDebug (1, $"DONE REWRITING CONDITIONALS: {Method.Name}");

			Context.Options.OptimizerReport.MarkAsContainingConditionals (Method);

			EliminateDeadBlocks ();
		}

		void RewriteLinkerConditional (BasicBlock block)
		{
			LogDebug (1, $"REWRITE LINKER CONDITIONAL: {block.LinkerConditional}");

			DumpBlocks ();

			var conditional = block.LinkerConditional;
			block.LinkerConditional = null;

			conditional.RewriteConditional (ref block);

			BlockList.ComputeOffsets ();

			DumpBlocks ();

			LogDebug (1, $"DONE REWRITING LINKER CONDITIONAL");
		}

		void EliminateDeadBlocks (bool full = true)
		{
			LogDebug (1, $"ELIMINATING DEAD BLOCKS");

			bool removed;
			bool first = true;

			do {
				if (first)
					first = false;
				else
					Scan ();

				var flow = new FlowAnalysis (this);
				flow.Analyze ();

				removed = false;
				var eliminator = new DeadCodeEliminator (this);
				removed |= eliminator.RemoveDeadBlocks ();
				if (full) {
					removed |= eliminator.RemoveDeadJumps ();
					removed |= eliminator.RemoveConstantJumps ();
				}
				removed |= eliminator.RemoveUnusedVariables ();

				if (removed)
					BlockList.ComputeOffsets ();

				LogDebug (1, $"ELIMINATING DEAD BLOCKS DONE: {removed}");
			} while (full && removed);
		}
	}
}
