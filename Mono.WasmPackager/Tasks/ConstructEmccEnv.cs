using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using Microsoft.Build.Framework;

namespace Mono.WasmPackager.Emscripten
{
	public class ConstructEmccEnv : Microsoft.Build.Utilities.Task
	{
		[Required]
		public string SdkDir {
			get; set;
		}

		[Required]
		public string EmsdkEnv {
			get; set;
		}

		[Required]
		public string ConfigFile {
			get; set;
		}

		public override bool Execute ()
		{
			Log.LogMessage (MessageImportance.High, $"CONSTRUCT EMCC ENV: {SdkDir} - {EmsdkEnv} - {ConfigFile}");

			var path = Environment.GetEnvironmentVariable ("PATH");
			var newPath = $"{path}:{SdkDir}";
			Log.LogMessage (MessageImportance.High, $"PATH: {path}");
			Log.LogMessage (MessageImportance.High, $"NEW PATH: {newPath}");

			using (var writer = new StreamWriter (EmsdkEnv)) {
				writer.WriteLine ($"export PATH=\"{path}:{SdkDir}\"");
				writer.WriteLine ($"export EMSDK=\"{SdkDir}\"");
				writer.WriteLine ($"export LLVM=\"{SdkDir}/upstream/bin\"");
				writer.WriteLine ($"export BINARYEN=\"{SdkDir}/upstream\"");
				writer.WriteLine ($"export EM_CONFIG=\"{ConfigFile}\"");
				writer.WriteLine ($"export EMSDK_NODE=\"{SdkDir}/node/12.9.1_64bit/bin/node\"");
			}

			return true;
		}
	}
}
