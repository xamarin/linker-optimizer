//
// IsTypeAvailableConditional.cs
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

	public class IsTypeAvailableConditional : LinkerConditional
	{
		public TypeDefinition ConditionalType {
			get;
		}

		public string ConditionalTypeName {
			get;
		}

		IsTypeAvailableConditional (BasicBlockScanner scanner, TypeDefinition type)
			: base (scanner)
		{
			ConditionalType = type;
		}

		IsTypeAvailableConditional (BasicBlockScanner scanner, string name)
			: base (scanner)
		{
			ConditionalTypeName = name;
		}

		bool EvaluateConditional ()
		{
			var type = ConditionalType ?? Context.Context.GetType (ConditionalTypeName);
			// It is legal to use MonoLinkerSupport.IsTypeAvailable(string) with undefined types.
			if (type == null)
				return false;

			Context.MarkConditionalType (type);
			return Context.Annotations.IsMarked (type);
		}

		public override void RewriteConditional (ref BasicBlock block)
		{
			var evaluated = EvaluateConditional ();

			Scanner.LogDebug (1, $"REWRITE TYPE AVAILABLE CONDITIONAL: {this} {evaluated}");

			RewriteConditional (ref block, 0, evaluated ? ConstantValue.True : ConstantValue.False);
		}

		public static IsTypeAvailableConditional Create (BasicBlockScanner scanner, ref BasicBlock bb, ref int index)
		{
			if (bb.Instructions.Count == 1)
				throw new OptimizerAssertionException ();
			if (index + 1 >= scanner.Method.Body.Instructions.Count)
				throw new OptimizerAssertionException ();

			/*
			 * `bool MonoLinkerSupport.IsTypeAvailable (string)`
			 *
			 */

			if (bb.Instructions.Count > 2)
				scanner.BlockList.SplitBlockAt (ref bb, bb.Instructions.Count - 2);

			if (bb.FirstInstruction.OpCode.Code != Code.Ldstr)
				throw new OptimizerAssertionException ($"Invalid argument `{bb.FirstInstruction}` used in `MonoLinkerSupport.IsTypeAvailable(string)` conditional.");

			var instance = new IsTypeAvailableConditional (scanner, (string)bb.FirstInstruction.Operand);
			bb.LinkerConditional = instance;

			LookAheadAfterConditional (scanner.BlockList, ref bb, ref index);

			return instance;
		}

		public static IsTypeAvailableConditional Create (BasicBlockScanner scanner, ref BasicBlock bb, ref int index, TypeDefinition type)
		{
			if (index + 1 >= scanner.Method.Body.Instructions.Count)
				throw new OptimizerAssertionException ();

			/*
			 * `bool MonoLinkerSupport.IsTypeAvailable<T> ()`
			 *
			 */

			if (bb.Instructions.Count > 1)
				scanner.BlockList.SplitBlockAt (ref bb, bb.Instructions.Count - 1);

			var instance = new IsTypeAvailableConditional (scanner, type);
			bb.LinkerConditional = instance;

			LookAheadAfterConditional (scanner.BlockList, ref bb, ref index);

			return instance;
		}

		public override string ToString ()
		{
			return $"[{GetType ().Name}: {ConditionalType?.ToString () ?? ConditionalTypeName}]";
		}
	}
}
