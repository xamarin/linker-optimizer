using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Build.Framework;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Mono.Linker.WasmPackager
{
	public class PackagerTask : Microsoft.Build.Utilities.Task
	{
		#region Task Properties

		public string MonoWasmRoot {
			get; set;
		}

		public string MonoSdkRoot {
			get; set;
		}

		public string EmscriptenSdkDir {
			get; set;
		}

		public string[] RootAssemblies {
			get; set;
		}

		public bool AddBinding {
			get; set;
		}

		public string BuildDir {
			get; set;
		}

		public bool EnableAOT {
			get; set;
		} = true;

		public bool EnableDedup {
			get; set;
		}

		public bool EnableThreads {
			get; set;
		}

		public bool UseReleaseRuntime {
			get; set;
		} = true;

		public string[] AotAssemblies {
			get; set;
		}

		public string RuntimeTemplate {
			get; set;
		} = "runtime.js";

		public string[] Assets {
			get; set;
		} = new string[0];

		public string[] Profilers {
			get; set;
		} = new string[0];

		public bool LinkICalls {
			get; set;
		}

		#endregion

		bool enable_debug, enable_linker;
		string app_prefix, framework_prefix, bcl_prefix, bcl_tools_prefix, bcl_facades_prefix, out_prefix;
		HashSet<string> asm_map = new HashSet<string> ();
		List<string> file_list = new List<string> ();
		HashSet<string> assemblies_with_dbg_info = new HashSet<string> ();
		List<string> root_search_paths = new List<string> ();

		const string BINDINGS_ASM_NAME = "WebAssembly.Bindings";
		const string BINDINGS_RUNTIME_CLASS_NAME = "WebAssembly.Runtime";
		const string HTTP_ASM_NAME = "WebAssembly.Net.Http";
		const string WEBSOCKETS_ASM_NAME = "WebAssembly.Net.WebSockets";
		const string BINDINGS_MODULE = "corebindings.o";
		const string BINDINGS_MODULE_SUPPORT = "$tool_prefix/src/binding_support.js";

		class AssemblyData
		{
			// Assembly name
			public string name;
			// Base filename
			public string filename;
			// Path outside build tree
			public string src_path;
			// Path of .bc file
			public string bc_path;
			// Path of the wasm object file
			public string o_path;
			// Path in appdir
			public string app_path;
			// Path of the AOT depfile
			public string aot_depfile_path;
			// Linker input path
			public string linkin_path;
			// Linker output path
			public string linkout_path;
			// AOT input path
			public string aotin_path;
			// Final output path after IL strip
			public string final_path;
			// Whenever to AOT this assembly
			public bool aot;
		}

		static List<AssemblyData> assemblies = new List<AssemblyData> ();

		enum AssemblyKind
		{
			User,
			Framework,
			Bcl,
			None,
		}

		static void Debug (string s)
		{
			Console.WriteLine (s);
		}

		static string FindFrameworkAssembly (string asm)
		{
			return asm;
		}

		static bool Try (string prefix, string name, out string out_res)
		{
			out_res = null;

			string res = (Path.Combine (prefix, name));
			if (File.Exists (res)) {
				out_res = Path.GetFullPath (res);
				return true;
			}
			return false;
		}

		static string ResolveWithExtension (string prefix, string name)
		{
			string res = null;

			if (Try (prefix, name, out res))
				return res;
			if (Try (prefix, name + ".dll", out res))
				return res;
			if (Try (prefix, name + ".exe", out res))
				return res;
			return null;
		}

		string ResolveUser (string asm_name)
		{
			return ResolveWithExtension (app_prefix, asm_name);
		}

		string ResolveFramework (string asm_name)
		{
			return ResolveWithExtension (framework_prefix, asm_name);
		}

		string ResolveBcl (string asm_name)
		{
			return ResolveWithExtension (bcl_prefix, asm_name);
		}

		string ResolveBclFacade (string asm_name)
		{
			return ResolveWithExtension (bcl_facades_prefix, asm_name);
		}

		string Resolve (string asm_name, out AssemblyKind kind)
		{
			kind = AssemblyKind.User;
			var asm = ResolveUser (asm_name);
			if (asm != null)
				return asm;

			kind = AssemblyKind.Framework;
			asm = ResolveFramework (asm_name);
			if (asm != null)
				return asm;

			kind = AssemblyKind.Bcl;
			asm = ResolveBcl (asm_name);
			if (asm == null)
				asm = ResolveBclFacade (asm_name);
			if (asm != null)
				return asm;

			kind = AssemblyKind.None;
			throw new Exception ($"Could not resolve {asm_name}");
		}

		void Import (string ra, AssemblyKind kind)
		{
			if (!asm_map.Add (ra))
				return;
			ReaderParameters rp = new ReaderParameters ();
			bool add_pdb = enable_debug && File.Exists (Path.ChangeExtension (ra, "pdb"));
			if (add_pdb) {
				rp.ReadSymbols = true;
				// Facades do not have symbols
				rp.ThrowIfSymbolsAreNotMatching = false;
				rp.SymbolReaderProvider = new DefaultSymbolReaderProvider (false);
			}

			var resolver = new DefaultAssemblyResolver ();
			root_search_paths.ForEach (resolver.AddSearchDirectory);
			resolver.AddSearchDirectory (bcl_facades_prefix);
			resolver.AddSearchDirectory (bcl_prefix);
			resolver.AddSearchDirectory (framework_prefix);
			rp.AssemblyResolver = resolver;

			rp.InMemory = true;
			var image = ModuleDefinition.ReadModule (ra, rp);
			file_list.Add (ra);
			//Debug ($"Processing {ra} debug {add_pdb}");

			var data = new AssemblyData () { name = image.Assembly.Name.Name, src_path = ra };
			assemblies.Add (data);

			if (add_pdb && (kind == AssemblyKind.User || kind == AssemblyKind.Framework)) {
				file_list.Add (Path.ChangeExtension (ra, "pdb"));
				assemblies_with_dbg_info.Add (Path.ChangeExtension (ra, "pdb"));
			}

			foreach (var ar in image.AssemblyReferences) {
				// Resolve using root search paths first
				var resolved = image.AssemblyResolver.Resolve (ar, rp);

				var searchName = resolved?.MainModule.FileName ?? ar.Name;

				var resolve = Resolve (searchName, out kind);
				Import (resolve, kind);
			}
		}

		void GenDriver (string builddir, List<string> profilers, ExecMode ee_mode, bool link_icalls)
		{
			var symbols = new List<string> ();
			foreach (var adata in assemblies) {
				if (adata.aot)
					symbols.Add (String.Format ("mono_aot_module_{0}_info", adata.name.Replace ('.', '_').Replace ('-', '_')));
			}

			var w = File.CreateText (Path.Combine (builddir, "driver-gen.c.in"));

			foreach (var symbol in symbols) {
				w.WriteLine ($"extern void *{symbol};");
			}

			w.WriteLine ("static void register_aot_modules ()");
			w.WriteLine ("{");
			foreach (var symbol in symbols)
				w.WriteLine ($"\tmono_aot_register_module ({symbol});");
			w.WriteLine ("}");

			foreach (var profiler in profilers) {
				w.WriteLine ($"void mono_profiler_init_{profiler} (const char *desc);");
				w.WriteLine ("EMSCRIPTEN_KEEPALIVE void mono_wasm_load_profiler_" + profiler + " (const char *desc) { mono_profiler_init_" + profiler + " (desc); }");
			}

			switch (ee_mode) {
				case ExecMode.AotInterp:
					w.WriteLine ("#define EE_MODE_LLVMONLY_INTERP 1");
					break;
				case ExecMode.Aot:
					w.WriteLine ("#define EE_MODE_LLVMONLY 1");
					break;
				default:
					break;
			}

			if (link_icalls)
				w.WriteLine ("#define LINK_ICALLS 1");

			w.Close ();
		}

		enum CopyType
		{
			Default,
			Always,
			IfNewer
		}

		enum ExecMode {
			Interp = 1,
			Aot = 2,
			AotInterp = 3
		}

		public override bool Execute ()
		{
			// NETCORE SAMPLE
			// mono --debug packager.exe --debugrt --emscripten-sdkdir=$(EMSCRIPTEN_SDK_DIR) --mono-sdkdir=$(TOP)/sdks/out -appdir=bin/hello-netcore --nobinding --builddir=obj/hello-netcore --framework=netcoreapp3.0 --netcore-sdkdir=$(NETCOREAPP_DIR) --template=runtime-tests.js --linker samples/hello/bin/Debug/netcoreapp3.0/hello.dll

			// AOT SAMPLE
			// mono --debug packager.exe --emscripten-sdkdir=$(EMSCRIPTEN_SDK_DIR) --mono-sdkdir=$(TOP)/sdks/out -appdir=bin/aot-sample --nobinding --builddir=obj/aot-sample --aot --template=runtime-tests.js --pinvoke-libs=libfoo hello.exe

			Log.LogMessage (MessageImportance.High, $"PACKAGER TASK: {MonoWasmRoot} {RootAssemblies.Length}");

			var deploy_prefix = "managed";
			var vfs_prefix = "managed";
			var emit_ninja = false;
			var copyType = CopyType.Default;
			var ee_mode = ExecMode.Interp;
			var build_wasm = EnableAOT;

			if (EnableAOT)
				ee_mode = ExecMode.AotInterp;

			out_prefix = Environment.CurrentDirectory;
			app_prefix = Environment.CurrentDirectory;

			if (MonoSdkRoot == null)
				MonoSdkRoot = Path.Combine (MonoWasmRoot, "sdks", "out");

			var tool_prefix = Path.Combine (MonoWasmRoot, "sdk", "wasm");
			framework_prefix = Path.Combine (MonoWasmRoot, "sdk", "wasm", "framework");
			bcl_prefix = Path.Combine (MonoSdkRoot, "wasm-bcl/wasm");
			bcl_tools_prefix = Path.Combine (MonoSdkRoot, "wasm-bcl/wasm_tools");
			bcl_facades_prefix = Path.Combine (bcl_prefix, "Facades");

			foreach (var ra in RootAssemblies) {
				AssemblyKind kind;
				var resolved = Resolve (ra, out kind);
				Import (resolved, kind);
			}

			if (AddBinding) {
				var bindings = ResolveFramework (BINDINGS_ASM_NAME + ".dll");
				Import (bindings, AssemblyKind.Framework);
				var http = ResolveFramework (HTTP_ASM_NAME + ".dll");
				Import (http, AssemblyKind.Framework);
				var websockets = ResolveFramework (WEBSOCKETS_ASM_NAME + ".dll");
				Import (websockets, AssemblyKind.Framework);
			}

			if (EnableAOT) {
				var to_aot = new Dictionary<string, bool> ();
				to_aot["mscorlib"] = true;
				if (AotAssemblies != null) {
					foreach (var s in AotAssemblies)
						to_aot[s] = true;
				}
				foreach (var ass in assemblies) {
					if (AotAssemblies == null || to_aot.ContainsKey (ass.name)) {
						ass.aot = true;
						to_aot.Remove (ass.name);
					}
				}
				if (to_aot.Count > 0) {
					Log.LogError ($"Unknown assembly name '{to_aot.Keys.ToArray ()[0]}' in --aot-assemblies option.");
					return false;
				}
			}

			if (BuildDir != null) {
				emit_ninja = true;
				if (!Directory.Exists (BuildDir))
					Directory.CreateDirectory (BuildDir);
			}

			if (!emit_ninja) {
				if (!Directory.Exists (out_prefix))
					Directory.CreateDirectory (out_prefix);
				var bcl_dir = Path.Combine (out_prefix, deploy_prefix);
				if (Directory.Exists (bcl_dir))
					Directory.Delete (bcl_dir, true);
				Directory.CreateDirectory (bcl_dir);
				foreach (var f in file_list) {
					CopyFile (f, Path.Combine (bcl_dir, Path.GetFileName (f)), copyType);
				}
			}

			if (deploy_prefix.EndsWith ("/"))
				deploy_prefix = deploy_prefix.Substring (0, deploy_prefix.Length - 1);
			if (vfs_prefix.EndsWith ("/"))
				vfs_prefix = vfs_prefix.Substring (0, vfs_prefix.Length - 1);

			// the linker does not consider these core by default
			var wasm_core_assemblies = new Dictionary<string, bool> ();
			if (AddBinding) {
				wasm_core_assemblies[BINDINGS_ASM_NAME] = true;
				wasm_core_assemblies[HTTP_ASM_NAME] = true;
				wasm_core_assemblies[WEBSOCKETS_ASM_NAME] = true;
			}
			// wasm core bindings module
			var wasm_core_bindings = string.Empty;
			if (AddBinding) {
				wasm_core_bindings = BINDINGS_MODULE;
			}
			// wasm core bindings support file
			var wasm_core_support = string.Empty;
			var wasm_core_support_library = string.Empty;
			if (AddBinding) {
				wasm_core_support = BINDINGS_MODULE_SUPPORT;
				wasm_core_support_library = $"--js-library {BINDINGS_MODULE_SUPPORT}";
			}

#if REMOVED
			var runtime_js = Path.Combine (emit_ninja ? BuildDir : out_prefix, "runtime.js");
			if (emit_ninja) {
				File.Delete (runtime_js);
				File.Copy (Path.Combine (MonoWasmRoot, "sdks", "wasm", RuntimeTemplate), runtime_js);
			} else {
				if (File.Exists (runtime_js) && (File.Exists (RuntimeTemplate))) {
					CopyFile (RuntimeTemplate, runtime_js, CopyType.IfNewer, $"runtime template <{RuntimeTemplate}> ");
				} else {
					if (File.Exists (RuntimeTemplate))
						CopyFile (RuntimeTemplate, runtime_js, CopyType.IfNewer, $"runtime template <{RuntimeTemplate}> ");
					else {
						var runtime_gen = "\nvar Module = {\n\tonRuntimeInitialized: function () {\n\t\tMONO.mono_load_runtime_and_bcl (\n\t\tconfig.vfs_prefix,\n\t\tconfig.deploy_prefix,\n\t\tconfig.enable_debugging,\n\t\tconfig.file_list,\n\t\tfunction () {\n\t\t\tApp.init ();\n\t\t}\n\t)\n\t},\n};";
						File.Delete (runtime_js);
						File.WriteAllText (runtime_js, runtime_gen);
					}
				}
			}
#endif

			AssemblyData dedup_asm = null;

			if (EnableDedup) {
				dedup_asm = new AssemblyData
				{
					name = "aot-dummy",
					filename = "aot-dummy.dll",
					bc_path = "$builddir/aot-dummy.dll.bc",
					o_path = "$builddir/aot-dummy.dll.o",
					app_path = "$appdir/$deploy_prefix/aot-dummy.dll",
					linkout_path = "$builddir/linker-out/aot-dummy.dll",
					aot = true
				};
				assemblies.Add (dedup_asm);
				file_list.Add ("aot-dummy.dll");
			}

			var file_list_str = string.Join (",", file_list.Select (f => $"\"{Path.GetFileName (f)}\"").Distinct ());
			var config = String.Format ("config = {{\n \tvfs_prefix: \"{0}\",\n \tdeploy_prefix: \"{1}\",\n \tenable_debugging: {2},\n \tfile_list: [ {3} ],\n", vfs_prefix, deploy_prefix, enable_debug ? "1" : "0", file_list_str);
			config += "}\n";
			var config_js = Path.Combine (emit_ninja ? BuildDir : out_prefix, "mono-config.js");
			File.Delete (config_js);
			File.WriteAllText (config_js, config);

			string runtime_dir;
			if (EnableThreads)
				runtime_dir = Path.Combine (tool_prefix, UseReleaseRuntime ? "builds/threads-release" : "builds/threads-debug");
			else
				runtime_dir = Path.Combine (tool_prefix, UseReleaseRuntime ? "builds/release" : "builds/debug");
			if (!emit_ninja) {
				var interp_files = new List<string> { "mono.js", "mono.wasm" };
				if (EnableThreads) {
					interp_files.Add ("mono.worker.js");
					interp_files.Add ("mono.js.mem");
				}
				foreach (var fname in interp_files) {
					File.Delete (Path.Combine (out_prefix, fname));
					File.Copy (
						Path.Combine (runtime_dir, fname),
						Path.Combine (out_prefix, fname));
				}

				foreach (var asset in Assets) {
					CopyFile (asset, Path.Combine (out_prefix, Path.GetFileName (asset)), copyType, "Asset: ");
				}
			}

			if (!emit_ninja)
				return true;

			if (build_wasm) {
				if (MonoSdkRoot == null) {
					Log.LogError ("The `MonoSdkRoot` argument is required.");
					return false;
				}
				if (EmscriptenSdkDir == null) {
					Log.LogError ("The `EmscriptenSdkDir` argument is required.");
					return false;
				}
				GenDriver (BuildDir, Profilers.ToList (), ee_mode, LinkICalls);
			}

			Log.LogMessage ("PACKAGER DONE!");

			return true;
		}

		static void CopyFile (string sourceFileName, string destFileName, CopyType copyType, string typeFile = "")
		{
			Console.WriteLine ($"{typeFile}cp: {copyType} - {sourceFileName} -> {destFileName}");
			switch (copyType) {
				case CopyType.Always:
					File.Copy (sourceFileName, destFileName, true);
					break;
				case CopyType.IfNewer:
					if (!File.Exists (destFileName)) {
						File.Copy (sourceFileName, destFileName);
					} else {
						var srcInfo = new FileInfo (sourceFileName);
						var dstInfo = new FileInfo (destFileName);

						if (srcInfo.LastWriteTime.Ticks > dstInfo.LastWriteTime.Ticks || srcInfo.Length > dstInfo.Length)
							File.Copy (sourceFileName, destFileName, true);
						else
							Console.WriteLine ($"    skipping: {sourceFileName}");
					}
					break;
				default:
					File.Copy (sourceFileName, destFileName);
					break;
			}
		}
	}
}
