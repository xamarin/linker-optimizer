using System.IO;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Build.Framework;

namespace Mono.Linker.WasmPackager
{
	public class GeneratePInvokeTable : Microsoft.Build.Utilities.Task
	{
		[Required]
		public string[] NativeLibraries {
			get; set;
		}

		[Required]
		public string[] Assemblies {
			get; set;
		}

		[Required]
		public string OutputFile {
			get; set;
		}

		public override bool Execute ()
		{
			Log.LogMessage (MessageImportance.High, $"WasmPackager - generating P/Invoke table: {OutputFile}");
			Log.LogArray (NativeLibraries, "  NativeLibraries");
			Log.LogArray (Assemblies, "  Assemblies");

			var tuner = new WasmTuner ();
			using (var writer = new StreamWriter (OutputFile)) {
				tuner.GeneratePInvokeTable (NativeLibraries, Assemblies, writer);
			}
			return true;
		}
	}
}
