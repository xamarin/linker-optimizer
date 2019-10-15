using System.IO;
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
			var tuner = new WasmTuner ();
			using (var writer = new StreamWriter (OutputFile)) {
				tuner.GeneratePInvokeTable (NativeLibraries, Assemblies, writer);
			}
			return true;
		}
	}
}
