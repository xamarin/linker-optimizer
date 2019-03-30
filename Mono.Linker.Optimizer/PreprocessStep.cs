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
using Mono.Cecil;

namespace Mono.Linker.Optimizer
{
	using BasicBlocks;

	public class PreprocessStep : ConditionalBaseStep
	{
		public PreprocessStep (MartinContext context)
			: base (context)
		{
		}

		protected override void ProcessAssembly (AssemblyDefinition assembly)
		{
			foreach (var type in assembly.MainModule.Types) {
				ProcessType (type);
			}
		}

		protected override void EndProcess ()
		{
			DumpConstantProperties ();
		}

		void ProcessType (TypeDefinition type)
		{
			Context.Options.ProcessTypeEntries (type, a => ProcessTypeActions (type, a));

			if (type.HasNestedTypes) {
				foreach (var nested in type.NestedTypes)
					ProcessType (nested);
			}

			foreach (var method in type.Methods) {
				ProcessMethod (method);
			}

			foreach (var property in type.Properties) {
				ProcessProperty (property);
			}
		}

		void ProcessMethod (MethodDefinition method)
		{
			Context.Options.ProcessMethodEntries (method, a => ProcessMethodActions (method, a));
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

			case OptimizerOptions.MethodAction.ReturnNull:
				CodeRewriter.ReplaceWithReturnNull (Context, method);
				break;
			}
		}

		void DumpConstantProperties ()
		{
			var writer = new XmlConfigurationWriter (Context);
			writer.DumpConstantProperties ();
		}
	}
}
