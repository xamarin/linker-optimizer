using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using Microsoft.Build.Framework;

namespace Mono.WasmPackager
{
	public class CompileAot : Microsoft.Build.Utilities.Task
	{
		[Required]
		public string MonoCrossBin {
			get; set;
		}

		public string MonoPath {
			get; set;
		}

		[Required]
		public string AotArgs {
			get; set;
		}

		[Required]
		public string[] Inputs {
			get; set;
		}

		public string Output {
			get; set;
		}

		public override bool Execute ()
		{
			var psi = new ProcessStartInfo (Path.Combine (MonoCrossBin));
			psi.UseShellExecute = false;

			if (!string.IsNullOrEmpty (MonoPath))
				psi.EnvironmentVariables["MONO_PATH"] = MonoPath;

			var arguments = new List<string> ();
			arguments.Add ("--debug");
			arguments.Add ($"--aot={AotArgs}");
			arguments.AddRange (Inputs);

			psi.Arguments = string.Join (" ", arguments);

			Log.LogMessage (MessageImportance.Normal, $"Invoking Mono with arguments: {psi.Arguments}");

			var proc = Process.Start (psi);
			proc.WaitForExit ();

			return proc.ExitCode == 0;
		}
	}
}
