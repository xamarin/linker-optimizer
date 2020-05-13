using System;
using System.IO;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Mono.WasmPackager
{
	static class AutoGenFileHelper 
	{
		public static void CreateFile (string output, Action<TextWriter> action, TaskLoggingHelper logger = null)
		{
			var writer = new StringWriter ();
			writer.WriteLine ("// AUTO-GENERATED FILE - DO NOT MODIFY!");
			action (writer);

			writer.Flush ();

			var text = writer.ToString ();

			if (!File.Exists (output)) {
				logger?.LogMessage (MessageImportance.Normal, $"Output file {output} does not exist, creating it.");
				File.WriteAllText (output, text);
				return;
			}

			var existing = File.ReadAllText (output);
			if (existing.Equals (text)) {
				logger?.LogMessage (MessageImportance.Normal, $"Output file {output} is already up-to-date.");
				return;
			}

			logger?.LogMessage (MessageImportance.Normal, $"Updating output file {output}.");
			File.WriteAllText (output, text);
		}
	}
}
