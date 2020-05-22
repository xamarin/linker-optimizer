using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using Microsoft.Build.Framework;

namespace Mono.WasmPackager.Emscripten
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

		public string Input {
			get; set;
		}

		public string CacheDir {
			get; set;
		}

		public string ConfigFile {
			get; set;
		}

		public string Output {
			get; set;
		}

		public string Flags {
			get; set;
		}

		public bool Quiet {
			get; set;
		}

		public override bool Execute ()
		{
			Log.LogMessage (MessageImportance.High, $"EMCC: {Input} - {Output} - {Flags}");
			Log.LogMessage (MessageImportance.Normal, $"  EmsdkEnv: {EmsdkEnv}");

			var environment = SourceEnvironment.ParseEnvironmentVariables (Log, EmsdkEnv, false);

			var emsdkPath = Path.Combine (SdkDir, "upstream", "emscripten", "emcc");
			Log.LogMessage (MessageImportance.High, $"  Emcc: {emsdkPath}");

			var psi = new ProcessStartInfo (emsdkPath);
			psi.UseShellExecute = false;
			foreach (var var in environment) {
				Log.LogMessage (MessageImportance.Normal, $"  ENV: {var.Key} = {var.Value}");
				psi.EnvironmentVariables[var.Key] = var.Value;
			}

			if (!string.IsNullOrEmpty (CacheDir)) {
				Log.LogMessage (MessageImportance.Normal, $"  Cache Dir: {CacheDir}");
				psi.EnvironmentVariables["EM_CACHE"] = CacheDir;
			}
			if (!string.IsNullOrEmpty (ConfigFile)) {
				Log.LogMessage (MessageImportance.Normal, $"  Config File: {ConfigFile}");
				psi.EnvironmentVariables["EM_CONFIG"] = ConfigFile;
			}

			if (string.IsNullOrEmpty (Output))
				Output = Path.ChangeExtension (Input, ".o");

			var arguments = new List<string> ();
			if (!string.IsNullOrEmpty (Input)) {
				arguments.Add (Input);
				arguments.Add ($"-o {Output}");
			}

			arguments.Add (Flags);
			arguments.Add ("--verbose");

			psi.Arguments = string.Join (" ", arguments);

			Log.LogMessage (MessageImportance.High, $"Invoking emcc with arguments: {psi.Arguments}");

			if (Quiet) {
				psi.RedirectStandardOutput = true;
			}

			var proc = Process.Start (psi);
			proc.WaitForExit ();

			return proc.ExitCode == 0;
		}
	}
}
