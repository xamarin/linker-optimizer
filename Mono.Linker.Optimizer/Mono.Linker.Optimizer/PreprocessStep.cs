﻿//
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

namespace Mono.Linker.Optimizer
{
	using BasicBlocks;
	using Configuration;

	public class PreprocessStep : OptimizerBaseStep
	{
		public PreprocessStep (OptimizerContext context)
			: base (context)
		{
		}

		protected override void Process ()
		{
			RemoveFeatures ();

			Context.LogDebug ($"Preprocessor mode: {Options.Preprocessor}.");

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

		readonly static string[] MonoCollationResources = {
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
			ActionVisitor.Visit (Options, type, a => ProcessTypeActions (type, a));

			if (false && type.HasNestedTypes) {
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
			ActionVisitor.Visit (Options, method, a => ProcessMethodActions (method, a));
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

		void ProcessTypeActions (TypeDefinition type, Type entry)
		{
			Context.AddTypeEntry (type, entry);

			switch (entry.Action) {
			case TypeAction.Debug:
				Context.LogMessage (MessageImportance.High, $"Debug type: {type} {entry}");
				Context.Debug ();
				break;

			case TypeAction.Preserve:
				Context.Annotations.SetPreserve (type, TypePreserve.All);
				Context.Annotations.Mark (type, DependencyInfo.Unspecified);
				break;
			}
		}

		void ProcessMethodActions (MethodDefinition method, Method entry)
		{
			Context.AddMethodEntry (method, entry);

			switch (entry.Action ?? MethodAction.None) {
			case MethodAction.Debug:
				Context.LogMessage (MessageImportance.High, $"Debug method: {method}");
				Context.Debug ();
				break;

			case MethodAction.Throw:
				CodeRewriter.ReplaceWithPlatformNotSupportedException (Context, method);
				break;

			case MethodAction.ReturnFalse:
				CodeRewriter.ReplaceWithReturnFalse (Context, method);
				break;

			case MethodAction.ReturnTrue:
				CodeRewriter.ReplaceWithReturnTrue (Context, method);
				break;

			case MethodAction.ReturnNull:
				CodeRewriter.ReplaceWithReturnNull (Context, method);
				break;
			}
		}
	}
}
