using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using Microsoft.Build.Framework;

namespace Mono.Linker.WasmPackager
{
	public class Emcc : Microsoft.Build.Utilities.Task
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
		public string BuildDir {
			get; set;
		}

		[Required]
		public string Input {
			get; set;
		}

		public string Output {
			get; set;
		}

		public string Flags {
			get; set;
		}

		public override bool Execute ()
		{
			Log.LogMessage (MessageImportance.High, $"EMCC: |{Flags}|");

			var environment = SourceEnvironment.ParseEnvironmentVariables (Log, EmsdkEnv, false);

			var psi = new ProcessStartInfo (Path.Combine (SdkDir, "upstream", "emscripten", "emcc"));
			psi.UseShellExecute = false;
			foreach (var var in environment)
				psi.EnvironmentVariables[var.Key] = var.Value;

			if (string.IsNullOrEmpty (Output))
				Output = Path.ChangeExtension (Input, ".o");

			var arguments = new List<string> ();
			arguments.Add (Input);
			arguments.Add (Flags);
			arguments.Add ("-c");
			arguments.Add ($"-o {Output}");
			arguments.Add ("--verbose");

			psi.Arguments = string.Join (" ", arguments);

			Log.LogMessage (MessageImportance.High, $"Invoking emcc with arguments: {psi.Arguments}");

			var proc = Process.Start (psi);
			proc.WaitForExit ();

			return proc.ExitCode == 0;
		}
	}
}
