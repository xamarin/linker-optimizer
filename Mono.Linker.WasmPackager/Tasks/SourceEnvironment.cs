using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.Build.Shared;

namespace Mono.Linker.WasmPackager
{
	public class SourceEnvironment : Microsoft.Build.Utilities.Task
	{
		[Required]
		public string InputFile {
			get; set;
		}

		[Output]
		public ITaskItem EnvironmentVariables {
			get; set;
		}

		[Output]
		public ITaskItem[] AllEnvironmentVariables {
			get; set;
		}

		static readonly Regex Regex = new Regex ("^export\\s+(\\w+)\\s*=\"(.*)\"$");

		public static Dictionary<string,string> ParseEnvironmentVariables (TaskLoggingHelper logger, string inputFileName, bool escape)
		{
			var vars = new Dictionary<string, string> ();
			foreach (var line in File.ReadAllLines (inputFileName)) {
				var match = Regex.Match (line);
				if (!match.Success) {
					logger.LogError ($"Failed to parse environment export: '{line}'.");
					return null;
				}

				var name = match.Groups[1].Value;
				var value = escape ? EscapingUtilities.Escape (match.Groups[2].Value) : match.Groups[2].Value;
				vars.Add (name, value);
			}

			return vars;
		}

		public override bool Execute ()
		{
			var vars = ParseEnvironmentVariables (Log, InputFile, true);
			if (vars == null)
				return false;

			var allEnvVars = new List<ITaskItem> ();
			foreach (var entry in vars) {
				allEnvVars.Add (new TaskItem (entry.Key, new Dictionary<string, string> { { "Value", entry.Value } }));
			}

			EnvironmentVariables = new TaskItem ("ExtraEnvironmentVariables", vars);
			AllEnvironmentVariables = allEnvVars.ToArray ();

			return true;
		}
	}
}
