//
// BlazorPreserveStep.cs
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
using Mono.Cecil;

namespace Mono.Linker.Optimizer
{
	public class BlazorPreserveStep : OptimizerBaseStep
	{
		public BlazorPreserveStep (OptimizerContext context) : base (context)
		{
		}

		const string jsinterop_name = "Microsoft.JSInterop";
		const string jsinvokable_name = "Microsoft.JSInterop.JSInvokableAttribute";

		TypeDefinition js_invokable_attribute_type;

		protected override void Process ()
		{
			Initialize ();

			PreserveAssembly (Context.MainAssembly);

			foreach (var assembly in GetAssemblies ()) {
				foreach (var type in assembly.MainModule.Types) {
					ProcessType (type);
				}
			}
		}

		void Initialize ()
		{
			var asm = GetAssemblies ().FirstOrDefault (a => a.Name.Name.Equals (jsinterop_name));
			if (asm == null) {
				Context.LogWarning ($"Cannot resolve `{jsinterop_name}'.");
				return;
			}

			js_invokable_attribute_type = asm.MainModule.GetType (jsinvokable_name);
			if (js_invokable_attribute_type == null)
				Context.LogWarning ($"Cannot find `{jsinvokable_name}' in assembly `{asm}'.");
		}

		void PreserveAssembly (AssemblyDefinition assembly)
		{
			foreach (var type in assembly.MainModule.Types)
				MarkAndPreserveAll (type);
		}

		void MarkAndPreserveAll (TypeDefinition type)
		{
			Annotations.MarkAndPush (type);
			Annotations.SetPreserve (type, TypePreserve.All);

			if (type.HasNestedTypes) {
				foreach (var nested in type.NestedTypes)
					MarkAndPreserveAll (nested);
			}
		}

		void ProcessType (TypeDefinition type)
		{
			if (type.HasNestedTypes) {
				foreach (var nested in type.NestedTypes)
					ProcessType (nested);
			}

			foreach (var method in type.Methods) {
				ProcessMethod (method);
			}
		}

		bool IsJsInvokableAttribute (CustomAttribute cattr)
		{
			var type = cattr.AttributeType.Resolve ();
			if (type == null)
				return false;

			return type.Equals (js_invokable_attribute_type);
		}

		void ProcessMethod (MethodDefinition method)
		{
			if (method.CustomAttributes.Any (IsJsInvokableAttribute))
				PreserveJSInvokable (method);
		}

		void PreserveJSInvokable (MethodDefinition method)
		{
			Context.LogDebug ($"Preserving `[{js_invokable_attribute_type}]': `{method}' in `{method.DeclaringType.Module.FileName}'.");

			PreserveType (method.DeclaringType);
			Annotations.AddPreservedMethod (method.DeclaringType, method);

			if (method.HasParameters) {
				foreach (var pd in method.Parameters) {
					var ptype = BasicBlocks.CecilHelper.Resolve (pd.ParameterType);
					if (ptype != null) {
						PreserveType (ptype);
						Annotations.SetPreserve (ptype, TypePreserve.All);
					}
				}
			}
		}

		void PreserveType (TypeDefinition type)
		{
			Annotations.Mark (type);
			if (type.IsNested)
				PreserveType (type.DeclaringType);
		}
	}
}
