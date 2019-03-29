//
// FlowAnalysis.cs
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
	public class FlowAnalysis
	{
		public BasicBlockScanner Scanner {
			get;
		}

		public BasicBlockList BlockList => Scanner.BlockList;

		public FlowAnalysis (BasicBlockScanner scanner)
		{
			Scanner = scanner;
		}

		protected MethodDefinition Method => BlockList.Body.Method;

		protected AssemblyDefinition Assembly => Method.DeclaringType.Module.Assembly;

		public void Analyze ()
		{
			Scanner.DumpBlocks ();

			Scanner.LogDebug (1, $"ANALYZE: {Method.Name}");

			if (Scanner.DebugLevel > 0)
				Scanner.Context.Debug ();

			var marked = new HashSet<BasicBlock> ();
			var reachable = true;

			var unresolved = new List<JumpOrigin> ();

			for (int i = 0; i < BlockList.Count; i++) {
				var block = BlockList [i];

				Scanner.LogDebug (2, $"#{i} ({(reachable ? "Reachable" : "Unreachable")}{(marked.Contains (block) ? ",Marked" : "")}): {block}");
				if (Scanner.DebugLevel > 2) {
					Scanner.LogDebug (2, "  ", null, block.JumpOrigins);
					Scanner.LogDebug (2, "  ", null, block.Instructions);
				}

				reachable |= marked.Contains (block);
				reachable |= block.Type == BasicBlockType.Finally;

				foreach (var origin in block.JumpOrigins) {
					BasicBlock origin_block;
					if (origin.Exception != null) {
						Scanner.LogDebug (2, $"  EXCEPTION ORIGIN: {origin}");
						if (block.FirstInstruction != origin.Exception.HandlerStart)
							continue;
						origin_block = BlockList.GetBlock (origin.Exception.TryStart);
						Scanner.LogDebug (2, $"  -> HANDLER START: {marked.Contains (origin_block)} {origin_block}");
					} else {
						origin_block = origin.OriginBlock;
					}

					if (marked.Contains (origin_block)) {
						Scanner.LogDebug (2, $"  MARKED ORIGIN: {origin}");
						reachable = true;
					} else if (!reachable) {
						Scanner.LogDebug (2, $"  UNRESOLVED ORIGIN: {origin}");
						unresolved.Add (origin);
					}
				}

				if (reachable)
					marked.Add (block);

				bool restart = false;
				for (int j = 0; j < unresolved.Count; j++) {
					if (unresolved [j].OriginBlock != block)
						continue;
					Scanner.LogDebug (2, $"  CHECK UNRESOLVED ({reachable}): {unresolved [j]}");
					if (!reachable)
						continue;
					var target = unresolved [j].Target;
					Scanner.LogDebug (2, $"  -> RESOLVE AND MARK: {target}");
					marked.Add (target);
					unresolved.RemoveAt (j--);

					if (i > target.Index) {
						i = target.Index - 1;
						restart = true;
					}
				}

				if (restart)
					continue;

				switch (block.BranchType) {
				case BranchType.None:
				case BranchType.Conditional:
				case BranchType.False:
				case BranchType.True:
				case BranchType.Switch:
					break;
				case BranchType.Exit:
				case BranchType.Return:
				case BranchType.Jump:
					reachable = false;
					break;
				}
			}

			Scanner.LogDebug (1, $"ANALYZE #1: {Method.Name}");

			for (int i = 0; i < BlockList.Count; i++) {
				if (!marked.Contains (BlockList [i]))
					BlockList [i].IsDead = true;
			}

			if (Scanner.DebugLevel > 2) {
				for (int i = 0; i < BlockList.Count; i++) {
					var block = BlockList [i];
					Scanner.LogDebug (2, $"#{i}{(marked.Contains (block) ? " (Marked)" : "")}: {block}");
					Scanner.LogDebug (2, "  ", null, block.JumpOrigins);
					Scanner.LogDebug (2, "  ", null, block.Instructions);
				}
			}

			Scanner.LogDebug (1, $"ANALYZE DONE: {Method.Name}");

			if (Scanner.DebugLevel > 0)
				Scanner.Context.Debug ();
		}
	}
}
