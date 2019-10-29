using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Mono.Linker.WasmPackager
{
	public class WasmResolverTask : Microsoft.Build.Utilities.Task
	{
		#region Task Properties

		[Required]
		public string MonoWasmRoot {
			get; set;
		}

		[Required]
		public string[] RootAssemblies {
			get; set;
		}

		public string Framework {
			get; set;
		}

		public string NetCoreAppDir {
			get; set;
		}

		public bool AddBinding {
			get; set;
		}

		public bool EnableAOT {
			get; set;
		}

		public bool EnableDebug {
			get; set;
		}

		public string[] AotAssemblies {
			get; set;
		}

		[Output]
		public ITaskItem[] FileList {
			get; set;
		}

		[Output]
		public ITaskItem[] Assemblies {
			get; set;
		}

		#endregion

		string app_prefix, framework_prefix, bcl_prefix, bcl_tools_prefix, bcl_facades_prefix, out_prefix;
		static List<string> bcl_prefixes;
		HashSet<string> asm_map = new HashSet<string> ();
		List<string> file_list = new List<string> ();
		HashSet<string> assemblies_with_dbg_info = new HashSet<string> ();
		List<string> root_search_paths = new List<string> ();
		bool is_netcore;

		List<AssemblyData> assemblies = new List<AssemblyData> ();

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
			public string src_path;
			// Whenever to AOT this assembly
			public bool aot;

			public TaskItem CreateTaskItem ()
			{
				return new TaskItem (src_path, new Dictionary<string, string> () {
					{ "Name", name },
					{ "AOT", aot ? "true" : "false" }
				});
			}
		}

		enum AssemblyKind
		{
			User,
			Framework,
			Bcl,
			None,
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
			foreach (var prefix in bcl_prefixes) {
				string res = ResolveWithExtension (prefix, asm_name);
				if (res != null)
					return res;
			}
			return null;
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
			bool add_pdb = EnableDebug && File.Exists (Path.ChangeExtension (ra, "pdb"));
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

		public override bool Execute ()
		{
			out_prefix = Environment.CurrentDirectory;
			app_prefix = Environment.CurrentDirectory;

			if (!string.IsNullOrEmpty (Framework)) {
				if (Framework.StartsWith ("netcoreapp")) {
					is_netcore = true;
					if (string.IsNullOrEmpty (NetCoreAppDir)) {
						Log.LogError ("The 'NetCoreAppDir' argument is required.");
						return false;
					}
				} else {
					Log.LogError ("The only valid value for 'Framework' is 'netcoreapp...'");
					return false;
				}
			}

			var mono_sdk_root = Path.Combine (MonoWasmRoot, "sdks", "out");

			var tool_prefix = Path.Combine (MonoWasmRoot, "sdk", "wasm");
			framework_prefix = Path.Combine (MonoWasmRoot, "sdk", "wasm", "framework");
			bcl_prefix = Path.Combine (mono_sdk_root, "wasm-bcl/wasm");
			bcl_tools_prefix = Path.Combine (mono_sdk_root, "wasm-bcl/wasm_tools");
			bcl_facades_prefix = Path.Combine (bcl_prefix, "Facades");
			bcl_prefixes = new List<string> ();
			if (is_netcore) {
				/* corelib */
				bcl_prefixes.Add (Path.Combine (mono_sdk_root, "netcore"));
				/* .net runtime */
				bcl_prefixes.Add (NetCoreAppDir);
			} else {
				bcl_prefixes.Add (bcl_prefix);
			}

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

			RemoveDuplicates (ref file_list, "file", f => f);
			RemoveDuplicates (ref assemblies, "assembly", a => a.name);

			FileList = file_list.Select (f => new TaskItem (f)).ToArray ();
			Assemblies = assemblies.Select (a => a.CreateTaskItem ()).ToArray ();

			return true;
		}

		void RemoveDuplicates<T> (ref List<T> list, string name, Func<T, string> getKey)
		{
			var dupsFound = false;
			var dict = new Dictionary<string, T> ();
			foreach (var item in list) {
				var key = getKey (item);
				if (dict.ContainsKey (key)) {
					Log.LogError ($"Duplicate {name}: '{key}'.");
					dupsFound = true;
				} else {
					dict.Add (key, item);
				}
			}

			if (dupsFound)
				list = dict.Values.ToList ();
		}
	}
}
