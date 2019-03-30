//
// LinkerConditional.cs
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

	public abstract class LinkerConditional
	{
		public BasicBlockScanner Scanner {
			get;
		}

		public BasicBlockList BlockList => Scanner.BlockList;

		protected MartinContext Context => Scanner.Context;

		protected MethodDefinition Method => BlockList.Body.Method;

		protected AssemblyDefinition Assembly => Method.DeclaringType.Module.Assembly;

		protected LinkerConditional (BasicBlockScanner scanner)
		{
			Scanner = scanner;
		}

		public abstract void RewriteConditional (ref BasicBlock block);

		protected void RewriteConditional (ref BasicBlock block, int stackDepth, ConstantValue constant)
		{
			if (constant == ConstantValue.Throw) {
				Scanner.Rewriter.ReplaceWithThrow (ref block, stackDepth);
				return;
			}

			/*
			 * The conditional call can be replaced with a constant.
			 */

			switch (block.BranchType) {
			case BranchType.False:
				switch (constant) {
				case ConstantValue.False:
				case ConstantValue.Null:
					Scanner.Rewriter.ReplaceWithBranch (ref block, stackDepth, true);
					break;
				case ConstantValue.True:
					Scanner.Rewriter.ReplaceWithBranch (ref block, stackDepth, false);
					break;
				default:
					throw DebugHelpers.AssertFailUnexpected (Method, block, block.BranchType);

				}
				break;

			case BranchType.True:
				switch (constant) {
				case ConstantValue.False:
				case ConstantValue.Null:
					Scanner.Rewriter.ReplaceWithBranch (ref block, stackDepth, false);
					break;
				case ConstantValue.True:
					Scanner.Rewriter.ReplaceWithBranch (ref block, stackDepth, true);
					break;
				default:
					throw DebugHelpers.AssertFailUnexpected (Method, block, block.BranchType);

				}
				break;

			case BranchType.None:
			case BranchType.Return:
				Scanner.Rewriter.ReplaceWithConstant (ref block, stackDepth, constant);
				break;

			default:
				throw DebugHelpers.AssertFailUnexpected (Method, block, block.BranchType);
			}
		}

		public static bool Scan (BasicBlockScanner scanner, ref BasicBlock bb, ref int index, Instruction instruction)
		{
			var reference = (MethodReference)instruction.Operand;
			var target = reference.Resolve ();
			if (target == null) {
				if (reference.DeclaringType.Name.Contains ("...")) {
					// FIXME: we don't support Ranges yet.
					return false;
				}
				scanner.Context.LogMessage (MessageImportance.High, $"Cannot resolve call target: {CecilHelper.Format (instruction)}");
				if (scanner.Context.Options.IgnoreResolutionErrors)
					return false;
				throw new ResolutionException (reference);
			}
			scanner.LogDebug (2, $"    CALL: {target}");

			if (instruction.Operand is GenericInstanceMethod genericInstance) {
				if (scanner.Context.IsWeakInstanceOfMethod (target)) {
					var conditionalType = genericInstance.GenericArguments [0].Resolve ();
					if (conditionalType == null)
						throw new ResolutionException (genericInstance.GenericArguments [0]);
					IsWeakInstanceOfConditional.Create (scanner, ref bb, ref index, conditionalType);
					return true;
				} else if (scanner.Context.AsWeakInstanceOfMethod (target)) {
					var conditionalType = genericInstance.GenericArguments [0].Resolve ();
					if (conditionalType == null)
						throw new ResolutionException (genericInstance.GenericArguments [0]);
					AsWeakInstanceOfConditional.Create (scanner, ref bb, ref index, conditionalType);
					return true;
				} else if (scanner.Context.IsTypeAvailableMethod (target)) {
					var conditionalType = genericInstance.GenericArguments [0].Resolve ();
					if (conditionalType == null)
						throw new ResolutionException (genericInstance.GenericArguments [0]);
					IsTypeAvailableConditional.Create (scanner, ref bb, ref index, conditionalType);
					return true;
				}
				return false;
			}

			if (scanner.Context.IsFeatureSupportedMethod (target)) {
				IsFeatureSupportedConditional.Create (scanner, ref bb, ref index);
				return true;
			}

			if (scanner.Context.IsTypeNameAvailableMethod (target)) {
				IsTypeAvailableConditional.Create (scanner, ref bb, ref index);
				return true;
			}


			if (scanner.Context.IsRequireFeatureMethod (target)) {
				RequireFeatureConditional.Create (scanner, ref bb, ref index);
				return true;
			}

			if (scanner.Context.TryGetConstantMethod (target, out var constant)) {
				ConstantCallConditional.Create (scanner, ref bb, ref index, target, constant);
				return true;
			}

			return false;
		}

		protected static void LookAheadAfterConditional (BasicBlockList blocks, ref BasicBlock bb, ref int index)
		{
			if (index + 1 >= blocks.Body.Instructions.Count)
				throw new NotSupportedException ();

			/*
			 * Look ahead at the instruction immediately following the call to the
			 * conditional support method (`IsWeakInstanceOf<T>()` or `IsFeatureSupported()`).
			 *
			 * If it's a branch, then we add it to the current block.  Since the conditional
			 * method leaves a `bool` value on the stack, the following instruction can never
			 * be an unconditional branch.
			 *
			 * At the end of this method, the current basic block will always look like this:
			 *
			 *   - (optional) simple load
			 *   - conditional call
			 *   - (optional) conditional branch.
			 *
			 * We will also close out the current block and start a new one after this.
			 */

			var next = blocks.Body.Instructions [index + 1];
			var type = CecilHelper.GetBranchType (next);

			switch (type) {
			case BranchType.None:
				bb = null;
				break;
			case BranchType.False:
			case BranchType.True:
				blocks.AddJumpOrigin (bb, next, (Instruction)next.Operand);
				goto case BranchType.Return;
			case BranchType.Return:
				bb.AddInstruction (next);
				index++;
				bb = null;
				break;
			default:
				throw DebugHelpers.AssertFailUnexpected (blocks.Method, bb, type);
			}
		}
	}
}
