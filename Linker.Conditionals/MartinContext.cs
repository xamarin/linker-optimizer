//
// MartinContext.cs
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
using Mono.Linker.Steps;

namespace Mono.Linker.Conditionals
{
	public class MartinContext
	{
		public LinkContext Context {
			get;
		}

		public MartinOptions Options {
			get;
		}

		public AnnotationStore Annotations => Context.Annotations;

		MartinContext (LinkContext context)
		{
			Context = context;
			Options = new MartinOptions ();
		}

		public void LogMessage (MessageImportance importance, string message)
		{
			Context.Logger.LogMessage (importance, message);
		}

		[Conditional ("DEBUG")]
		public void LogDebug (string message)
		{
			Context.Logger.LogMessage (MessageImportance.Low, message);
		}

		public static void InitializePipeline (LinkContext context)
		{
			context.Logger.LogMessage (MessageImportance.Normal, "Enabling Martin's Playground");

			context.MartinContext = new MartinContext (context);

			context.Pipeline.AddStepAfter (typeof (TypeMapStep), new InitializeStep ());
			context.Pipeline.AddStepAfter (typeof (InitializeStep), new PreprocessStep ());
			context.Pipeline.ReplaceStep (typeof (MarkStep), new ConditionalMarkStep ());
			context.Pipeline.AddStepAfter (typeof (OutputStep), new SizeReportStep ());
		}

		const string LinkerSupportType = "System.Runtime.CompilerServices.MonoLinkerSupport";
		const string IsTypeAvailableName = "System.Boolean " + LinkerSupportType + "::IsTypeAvailable()";
		const string IsTypeNameAvailableName = "System.Boolean " + LinkerSupportType + "::IsTypeAvailable(System.String)";

		TypeDefinition _corlib_support_type;
		TypeDefinition _test_helper_support_type;
		SupportMethodRegistration _is_weak_instance_of;
		SupportMethodRegistration _as_weak_instance_of;
		SupportMethodRegistration _is_feature_supported;
		SupportMethodRegistration _is_type_available;
		SupportMethodRegistration _is_type_name_available;
		SupportMethodRegistration _require_feature;
		Lazy<TypeDefinition> _platform_not_support_exception;
		Lazy<MethodDefinition> _platform_not_supported_exception_ctor;

		void Initialize ()
		{
			LogMessage (MessageImportance.High, "Initializing Martin's Playground");

			foreach (var asm in Context.GetAssemblies ()) {
				switch (asm.Name.Name) {
				case "mscorlib":
					_corlib_support_type = asm.MainModule.GetType (LinkerSupportType);
					break;
				case "TestHelpers":
					_test_helper_support_type = asm.MainModule.GetType (LinkerSupportType);
					break;
				}
			}

			if (_corlib_support_type == null)
				throw new NotSupportedException ($"Cannot find `{LinkerSupportType}` in corlib.");

			_is_weak_instance_of = ResolveSupportMethod ("IsWeakInstanceOf");
			_as_weak_instance_of = ResolveSupportMethod ("AsWeakInstanceOf");

			_is_feature_supported = ResolveSupportMethod ("IsFeatureSupported");
			_is_type_available = ResolveSupportMethod (IsTypeAvailableName, true);
			_is_type_name_available = ResolveSupportMethod (IsTypeNameAvailableName, true);
			_require_feature = ResolveSupportMethod ("RequireFeature");

			_platform_not_support_exception = new Lazy<TypeDefinition> (
				() => Context.GetType ("System.PlatformNotSupportedException") ?? throw new NotSupportedException ($"Can't find `System.PlatformNotSupportedException`."));
			_platform_not_supported_exception_ctor = new Lazy<MethodDefinition> (
				() => _platform_not_support_exception.Value.Methods.FirstOrDefault (m => m.Name == ".ctor") ?? throw new NotSupportedException ($"Can't find `System.PlatformNotSupportedException`."));

			Options.CheckEnvironmentOptions ();
		}

		SupportMethodRegistration ResolveSupportMethod (string name, bool full = false)
		{
			var corlib = _corlib_support_type.Methods.FirstOrDefault (m => full ? m.FullName == name : m.Name == name);
			if (corlib == null)
				throw new NotSupportedException ($"Cannot find `{LinkerSupportType}.{name}`.");

			var helper = _test_helper_support_type?.Methods.FirstOrDefault (m => full ? m.FullName == name : m.Name == name);
			return new SupportMethodRegistration (corlib, helper);
		}

		public TypeDefinition MonoLinkerSupportType {
			get;
			private set;
		}

		public bool IsWeakInstanceOfMethod (MethodDefinition method) => _is_weak_instance_of.Matches (method);

		public bool IsTypeAvailableMethod (MethodDefinition method) => _is_type_available.Matches (method);

		public bool IsTypeNameAvailableMethod (MethodDefinition method) => _is_type_name_available.Matches (method);

		public bool AsWeakInstanceOfMethod (MethodDefinition method) => _as_weak_instance_of.Matches (method);

		public bool IsFeatureSupportedMethod (MethodDefinition method) => _is_feature_supported.Matches (method);

		public bool IsRequireFeatureMethod (MethodDefinition method) => _require_feature.Matches (method);

		public bool IsEnabled (MethodDefinition method)
		{
			return Options.ScanAllModules || Options.EnableDebugging (method.DeclaringType);
		}

		internal int GetDebugLevel (MethodDefinition method)
		{
			return Options.EnableDebugging (method) ? 5 : 0;
		}

		internal void Debug ()
		{ }

		readonly HashSet<TypeDefinition> conditional_types = new HashSet<TypeDefinition> ();
		readonly Dictionary<MethodDefinition, ConstantValue> constant_methods = new Dictionary<MethodDefinition, ConstantValue> ();

		public bool IsConditionalTypeMarked (TypeDefinition type)
		{
			return conditional_types.Contains (type);
		}

		public void MarkConditionalType (TypeDefinition type)
		{
			conditional_types.Add (type);
		}

		internal void AttemptingToRedefineConditional (TypeDefinition type)
		{
			var message = $"Attempting to mark type `{type}` after it's already been used in a conditional!";
			LogMessage (MessageImportance.High, message);
			if (Options.NoConditionalRedefinition)
				throw new NotSupportedException (message);
		}

		internal void MarkAsConstantMethod (MethodDefinition method, ConstantValue value)
		{
			constant_methods.Add (method, value);
		}

		internal bool TryGetConstantMethod (MethodDefinition method, out ConstantValue value)
		{
			return constant_methods.TryGetValue (method, out value);
		}

		internal IList<MethodDefinition> GetConstantMethods ()
		{
			return constant_methods.Keys.ToList ();
		}

		internal Instruction CreateNewPlatformNotSupportedException (MethodDefinition method)
		{
			var reference = method.DeclaringType.Module.ImportReference (_platform_not_supported_exception_ctor.Value);
			return Instruction.Create (OpCodes.Newobj, reference);
		}

		class InitializeStep : BaseStep
		{
			protected override void Process ()
			{
				Context.MartinContext.Initialize ();
				base.Process ();
			}
		}

		class SupportMethodRegistration
		{
			public MethodDefinition Corlib {
				get;
			}

			public MethodDefinition Helper {
				get;
			}

			public SupportMethodRegistration (MethodDefinition corlib, MethodDefinition helper)
			{
				Corlib = corlib;
				Helper = helper;
			}

			public bool Matches (MethodDefinition method) => method != null && (method == Corlib || method == Helper);
		}
	}
}
