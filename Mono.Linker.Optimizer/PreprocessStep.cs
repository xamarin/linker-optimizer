//
// PreloadStep.cs
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
using System.Runtime.CompilerServices;

namespace Mono.Linker.Optimizer {
	using BasicBlocks;

	public class PreprocessStep : OptimizerBaseStep {
		public PreprocessStep (OptimizerContext context)
			: base (context)
		{
		}

		protected override void Process ()
		{
			RemoveFeatures ();

			Context.LogMessage (MessageImportance.Normal, $"Preprocessor mode: {Options.Preprocessor}.");

			switch (Options.Preprocessor) {
			case OptimizerOptions.PreprocessorMode.Automatic:
			case OptimizerOptions.PreprocessorMode.Full:
				Preprocess ();
				break;
			}
		}

		void RemoveFeatures ()
		{
			if (!Options.IsFeatureEnabled (MonoLinkerFeature.Collator))
				RemoveCollatorResources ();
		}

		readonly static string [] MonoCollationResources = {
			"collation.cjkCHS.bin",
			"collation.cjkCHT.bin",
			"collation.cjkJA.bin",
			"collation.cjkKO.bin",
			"collation.cjkKOlv2.bin",
			"collation.core.bin",
			"collation.tailoring.bin"
		};

		void RemoveCollatorResources ()
		{
			foreach (var assembly in GetAssemblies ()) {
				foreach (var res in MonoCollationResources)
					Annotations.AddResourceToRemove (assembly, res);
			}
		}

		void Preprocess ()
		{
			foreach (var assembly in GetAssemblies ()) {
				foreach (var type in assembly.MainModule.Types) {
					ProcessType (type);
				}
			}
		}

		void ProcessType (TypeDefinition type)
		{
			Options.ProcessTypeEntries (type, a => ProcessTypeActions (type, a));

			if (type.HasNestedTypes) {
				foreach (var nested in type.NestedTypes)
					ProcessType (nested);
			}

			foreach (var method in type.Methods) {
				ProcessMethod (method);
			}

			if (Options.Preprocessor == OptimizerOptions.PreprocessorMode.Full) {
				foreach (var property in type.Properties) {
					ProcessProperty (property);
				}
			}
		}

		void ProcessMethod (MethodDefinition method)
		{
			Options.ProcessMethodEntries (method, a => ProcessMethodActions (method, a));
		}

		void ProcessProperty (PropertyDefinition property)
		{
			if (property.SetMethod != null)
				return;
			if (property.GetMethod == null || !property.GetMethod.HasBody)
				return;
			if (property.PropertyType.MetadataType != MetadataType.Boolean)
				return;

			var scanner = BasicBlockScanner.Scan (Context, property.GetMethod);
			if (scanner == null || !scanner.FoundConditionals)
				return;

			Context.LogMessage (MessageImportance.Normal, $"Found conditional property: {property}");

			scanner.RewriteConditionals ();

			if (!CecilHelper.IsConstantLoad (scanner.Body, out var value)) {
				Context.LogMessage (MessageImportance.High, $"Property `{property}` uses conditionals, but does not return a constant.");
				return;
			}

			Context.MarkAsConstantMethod (property.GetMethod, value ? ConstantValue.True : ConstantValue.False);

			Context.Debug ();
		}

		void ProcessTypeActions (TypeDefinition type, OptimizerOptions.TypeAction action)
		{
			switch (action) {
			case OptimizerOptions.TypeAction.Debug:
				Context.LogMessage (MessageImportance.High, $"Debug type: {type} {action}");
				Context.Debug ();
				break;

			case OptimizerOptions.TypeAction.Preserve:
				Context.Annotations.SetPreserve (type, TypePreserve.All);
				Context.Annotations.Mark (type);
				break;
			}
		}

		void ProcessMethodActions (MethodDefinition method, OptimizerOptions.MethodAction action)
		{
			switch (action) {
			case OptimizerOptions.MethodAction.Debug:
				Context.LogMessage (MessageImportance.High, $"Debug method: {method} {action}");
				Context.Debug ();
				break;

			case OptimizerOptions.MethodAction.Throw:
				CodeRewriter.ReplaceWithPlatformNotSupportedException (Context, method);
				break;

			case OptimizerOptions.MethodAction.ReturnFalse:
				CodeRewriter.ReplaceWithReturnFalse (Context, method);
				break;

			case OptimizerOptions.MethodAction.ReturnTrue:
				CodeRewriter.ReplaceWithReturnTrue (Context, method);
				break;

			case OptimizerOptions.MethodAction.ReturnNull:
				CodeRewriter.ReplaceWithReturnNull (Context, method);
				break;
			}
		}
	}
}
