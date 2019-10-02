//
// OptimizerContext.cs
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
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Linker.Steps;

namespace Mono.Linker.Optimizer
{
	using Configuration;
	using BasicBlocks;

	public class OptimizerContext
	{
		public LinkContext Context {
			get;
		}

		public OptimizerOptions Options {
			get;
		}

		public string MainModule {
			get;
		}

		public AnnotationStore Annotations => Context.Annotations;

		OptimizerContext (LinkContext context, OptimizerOptions options, string mainModule)
		{
			Context = context;
			Options = options;
			MainModule = mainModule;
		}

		public void LogMessage (MessageImportance importance, string message)
		{
			Context.Logger.LogMessage (importance, message);
		}

		public void LogWarning (string message)
		{
			Context.Logger.LogMessage (MessageImportance.High, message);
		}

		[Conditional ("DEBUG")]
		public void LogDebug (string message)
		{
			Context.Logger.LogMessage (MessageImportance.Low, message);
		}

		static bool initialized;

		public static void Initialize (LinkContext linkContext, string mainModule, OptimizerOptions options)
		{
			if (initialized)
				return;
			initialized = true;

			var context = new OptimizerContext (linkContext, options, mainModule);

			if (options.CheckSize)
				options.ReportMode |= ReportMode.Size;

			if (options.OptimizerReport.IsEnabled (ReportMode.Detailed) || options.OptimizerReport.IsEnabled (ReportMode.Size)) {
				options.AnalyzeAll = options.ScanAllModules = true;
				if (options.Preprocessor == OptimizerOptions.PreprocessorMode.None)
					options.Preprocessor = OptimizerOptions.PreprocessorMode.Automatic;
			}

			linkContext.Pipeline.AddStepAfter (typeof (TypeMapStep), new InitializeOptimizerStep (context));
			if (options.EnableBlazor)
				linkContext.Pipeline.AddStepBefore(typeof(MarkStep), new BlazorPreserveStep(context));
			linkContext.Pipeline.AddStepBefore (typeof (MarkStep), new PreprocessStep (context));
			linkContext.Pipeline.ReplaceStep (typeof (MarkStep), new ConditionalMarkStep (context));
			linkContext.Pipeline.AppendStep (new SizeReportStep (context));
			if (options.CompareWith != null)
				linkContext.Pipeline.AppendStep (new CompareWithReportStep (context));
			linkContext.Pipeline.AppendStep (new GenerateReportStep (context));
			linkContext.Pipeline.AppendStep (new FinalCheckStep (context));

			context.ResolveAssemblies ();
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
		FieldInfo _tracer_stack_field;

		void ResolveAssemblies ()
		{
			foreach (var reference in Options.AssemblyReferences) {
				var assembly = Context.Resolve (reference);
				LogMessage (MessageImportance.Normal, $"Resolved assembly reference: {reference} {assembly}");
			}
		}

		void Initialize ()
		{
			LogMessage (MessageImportance.High, $"Initializing {Program.ProgramName}.");

			var mainName = Path.GetFileNameWithoutExtension (MainModule);

			foreach (var asm in Context.GetAssemblies ()) {
				switch (asm.Name.Name) {
				case "mscorlib":
					_corlib_support_type = asm.MainModule.GetType (LinkerSupportType);
					CorlibAssembly = asm;
					break;
				case "TestHelpers":
					_test_helper_support_type = asm.MainModule.GetType (LinkerSupportType);
					break;
				default:
					if (asm.Name.Name.Equals (mainName))
						MainAssembly = asm;
					break;
				}
			}

			if (CorlibAssembly == null) {
				Console.Error.WriteLine ($"Cannot find `mscorlib.dll` is assembly list.");
				// throw new OptimizerException ($"Cannot find `mscorlib.dll` is assembly list.");
			}
			if (MainAssembly == null)
				throw new OptimizerException ($"Cannot find main module `{MainModule}` is assembly list.");

			_is_weak_instance_of = ResolveSupportMethod ("IsWeakInstanceOf");
			_as_weak_instance_of = ResolveSupportMethod ("AsWeakInstanceOf");

			_is_feature_supported = ResolveSupportMethod ("IsFeatureSupported");
			_is_type_available = ResolveSupportMethod (IsTypeAvailableName, true);
			_is_type_name_available = ResolveSupportMethod (IsTypeNameAvailableName, true);
			_require_feature = ResolveSupportMethod ("RequireFeature");

			_platform_not_support_exception = new Lazy<TypeDefinition> (
				() => Context.GetType ("System.PlatformNotSupportedException") ?? throw new OptimizerException ($"Can't find `System.PlatformNotSupportedException`."));
			_platform_not_supported_exception_ctor = new Lazy<MethodDefinition> (
				() => _platform_not_support_exception.Value.Methods.FirstOrDefault (m => m.Name == ".ctor") ?? throw new OptimizerException ($"Can't find `System.PlatformNotSupportedException`."));

			_tracer_stack_field = typeof (Tracer).GetField ("dependency_stack", BindingFlags.Instance | BindingFlags.NonPublic);
		}

		SupportMethodRegistration ResolveSupportMethod (string name, bool full = false)
		{
			var corlib = _corlib_support_type?.Methods.FirstOrDefault (m => full ? m.FullName == name : m.Name == name);
			var helper = _test_helper_support_type?.Methods.FirstOrDefault (m => full ? m.FullName == name : m.Name == name);
			return new SupportMethodRegistration (corlib, helper);
		}

		public AssemblyDefinition CorlibAssembly {
			get;
			private set;
		}

		public AssemblyDefinition MainAssembly {
			get;
			private set;
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
			return Options.ScanAllModules || method.Module.Assembly == MainAssembly || Options.EnableDebugging (this, method.DeclaringType);
		}

		internal int GetDebugLevel (MethodDefinition method)
		{
			return Options.EnableDebugging (this, method) ? 5 : 0;
		}

		internal void Debug ()
		{ }

		readonly HashSet<TypeDefinition> conditional_types = new HashSet<TypeDefinition> ();
		readonly Dictionary<MethodDefinition, ConstantValue> constant_methods = new Dictionary<MethodDefinition, ConstantValue> ();
		readonly Dictionary<TypeDefinition, List<Type>> type_entries = new Dictionary<TypeDefinition, List<Type>> ();
		readonly Dictionary<MethodDefinition, List<Method>> method_entries = new Dictionary<MethodDefinition, List<Method>> ();

		public bool IsConditionalTypeMarked (TypeDefinition type)
		{
			return conditional_types.Contains (type);
		}

		public void MarkConditionalType (TypeDefinition type)
		{
			conditional_types.Add (type);
		}

		internal void AddTypeEntry (TypeDefinition type, Type entry)
		{
			if (!type_entries.TryGetValue (type, out var list)) {
				list = new List<Type> ();
				type_entries.Add (type, list);
			}
			list.Add (entry);
		}

		internal List<Type> GetTypeEntries (TypeDefinition type)
		{
			if (type_entries.TryGetValue (type, out var list))
				return list;
			return null;
		}

		internal void AddMethodEntry (MethodDefinition method, Method entry)
		{
			if (!method_entries.TryGetValue (method, out var list)) {
				list = new List<Method> ();
				method_entries.Add (method, list);
			}
			list.Add (entry);
		}

		internal List<Method> GetMethodEntries (MethodDefinition method)
		{
			if (method_entries.TryGetValue (method, out var list))
				return list;
			return null;
		}

		internal void AttemptingToRedefineConditional (TypeDefinition type)
		{
			var message = $"Attempting to mark type `{type}` after it's already been used in a conditional!";
			LogMessage (MessageImportance.High, message);
			if (Options.NoConditionalRedefinition)
				throw new OptimizerException (message);
		}

		internal void MarkAsConstantMethod (MethodDefinition method, ConstantValue value)
		{
			constant_methods.Add (method, value);
			Options.OptimizerReport?.MarkAsConstantMethod (method, value);
		}

		internal bool TryGetConstantMethod (MethodDefinition method, out ConstantValue value)
		{
			return constant_methods.TryGetValue (method, out value);
		}

		internal IList<MethodDefinition> GetConstantMethods ()
		{
			return constant_methods.Keys.ToList ();
		}

		internal bool TryFastResolve (MethodReference reference, out MethodDefinition resolved)
		{
			if (reference is MethodDefinition method) {
				resolved = method;
				return true;
			}

			resolved = null;
			if (reference.MetadataToken.TokenType != TokenType.MemberRef && reference.MetadataToken.TokenType != TokenType.MethodSpec)
				return false;

			if (reference.DeclaringType.IsNested || reference.DeclaringType.HasGenericParameters)
				return false;

			var type = reference.DeclaringType.Resolve ();
			if (type == null)
				return false;

			if (type == _corlib_support_type || type == _test_helper_support_type) {
				resolved = reference.Resolve ();
				return true;
			}

			return false;
		}

		internal Instruction CreateNewPlatformNotSupportedException (MethodDefinition method)
		{
			var reference = method.DeclaringType.Module.ImportReference (_platform_not_supported_exception_ctor.Value);
			return Instruction.Create (OpCodes.Newobj, reference);
		}

		static bool IsAssemblyBound (TypeDefinition td)
		{
			do {
				if (td.IsNestedPrivate || td.IsNestedAssembly || td.IsNestedFamilyAndAssembly)
					return true;

				td = td.DeclaringType;
			} while (td != null);

			return false;
		}

		static string TokenString (object o)
		{
			if (o == null)
				return "N:null";

			if (o is TypeReference t) {
				bool addAssembly = true;
				var td = t as TypeDefinition ?? t.Resolve ();

				if (td != null) {
					addAssembly = td.IsNotPublic || IsAssemblyBound (td);
					t = td;
				}

				var addition = addAssembly ? $":{t.Module}" : "";

				return $"{(o as IMetadataTokenProvider).MetadataToken.TokenType}:{o}{addition}";
			}

			if (o is IMetadataTokenProvider)
				return (o as IMetadataTokenProvider).MetadataToken.TokenType + ":" + o;

			return "Other:" + o;
		}

		internal List<string> DumpTracerStack ()
		{
			var list = new List<string> ();
			var stack = (Stack<object>)_tracer_stack_field.GetValue (Context.Tracer);
			if (stack == null)
				return list;
			LogMessage (MessageImportance.Normal, "Dependency Stack:");
			foreach (var dependency in stack) {
				var formatted = TokenString (dependency);
				list.Add (formatted);
				LogMessage (MessageImportance.Normal, "  " + formatted);
			}
			return list;
		}

		internal bool SizeCheckFailed {
			get; set;
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

		class InitializeOptimizerStep : OptimizerBaseStep
		{
			public InitializeOptimizerStep (OptimizerContext context)
				: base (context)
			{
			}

			protected override void Process ()
			{
				Context.Initialize ();
			}
		}

	}
}
