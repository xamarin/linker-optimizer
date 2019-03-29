//
// ConditionalMarkStep.cs
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
using Mono.Linker.Steps;
using System.Collections.Generic;
using Mono.Collections.Generic;

namespace Mono.Linker.Conditionals
{
	public class ConditionalMarkStep : MarkStep
	{
		public MartinContext MartinContext => _context.MartinContext;

		Queue<MethodDefinition> _conditional_methods;
		Dictionary<MethodDefinition, BasicBlockScanner> _block_scanner_by_method;

		public bool ProcessingConditionals {
			get;
			private set;
		}

		public ConditionalMarkStep ()
		{
			_conditional_methods = new Queue<MethodDefinition> ();
			_block_scanner_by_method = new Dictionary<MethodDefinition, BasicBlockScanner> ();
		}

		protected override void DoAdditionalProcessing ()
		{
			if (_methods.Count > 0)
				return;

			ProcessingConditionals = true;

			while (_conditional_methods.Count > 0) {
				var conditional = _conditional_methods.Dequeue ();

				Tracer.Push (conditional);

				var scanner = _block_scanner_by_method [conditional];
				if (scanner.DebugLevel > 0) {
					MartinContext.LogMessage (MessageImportance.Normal, $"CONDITIONAL METHOD: {conditional}");
					MartinContext.Debug ();
				}

				scanner.RewriteConditionals ();
				base.MarkMethodBody (conditional.Body);

				Tracer.Pop ();
			}
		}

		protected override void MarkMethodBody (MethodBody body)
		{
			if (!MartinContext.IsEnabled (body.Method)) {
				base.MarkMethodBody (body);
				return;
			}

			var debug = MartinContext.GetDebugLevel (body.Method);
			if (debug > 0) {
				MartinContext.LogMessage (MessageImportance.Normal, $"MARK BODY: {body.Method}");
				MartinContext.Debug ();
			}

			var scanner = BasicBlockScanner.Scan (MartinContext, body.Method);
			if (scanner == null) {
				MartinContext.LogDebug ($"BB SCAN FAILED: {body.Method}");
				base.MarkMethodBody (body);
				return;
			}

			if (scanner == null || !scanner.FoundConditionals) {
				base.MarkMethodBody (body);
				return;
			}

			if (debug > 0)
				MartinContext.LogDebug ($"MARK BODY - CONDITIONAL: {body.Method}");

			scanner.RewriteConditionals ();

			base.MarkMethodBody (body);
		}

		protected override TypeDefinition MarkType (TypeReference reference)
		{
			if (reference == null)
				return null;

			reference = GetOriginalType (reference);

			if (reference is FunctionPointerType)
				return null;

			if (reference is GenericParameter)
				return null;

			var type = ResolveTypeDefinition (reference);
			if (type == null) {
				HandleUnresolvedType (reference);
				return null;
			}

			if (Annotations.IsProcessed (type))
				return null;

			if (MartinContext.Options.EnableDebugging (type)) {
				MartinContext.LogMessage (MessageImportance.Normal, $"MARK TYPE: {type}");
				MartinContext.Debug ();
			}

			MartinContext.Options.CheckFailList (MartinContext, type);

			if (ProcessingConditionals && MartinContext.IsConditionalTypeMarked (type))
				MartinContext.AttemptingToRedefineConditional (type);

			return base.MarkType (reference);
		}

		protected override void EnqueueMethod (MethodDefinition method)
		{
			if (MartinContext.Options.EnableDebugging (method)) {
				MartinContext.LogMessage (MessageImportance.Normal, $"ENQUEUE METHOD: {method}");
				MartinContext.Debug ();
			}

			MartinContext.Options.CheckFailList (MartinContext, method);

			if (_conditional_methods.Contains (method))
				return;

			base.EnqueueMethod (method);
		}
	}
}
