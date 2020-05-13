using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.Build.Shared;

namespace Mono.WasmPackager
{
	public class GenerateTestSettings : Microsoft.Build.Utilities.Task
	{
		[Required]
		public bool IsTestSuite {
			get; set;
		}

		[Required]
		// Don't use string here as that wouldn't trim whitespace.
		public ITaskItem Output {
			get; set;
		}

		public ITaskItem DevServer_RootDir {
			get; set;
		}

		public ITaskItem DevServer_FrameworkDir {
			get; set;
		}

		public ITaskItem DevServer_Arguments {
			get; set;
		}

		public ITaskItem DevServer_Assembly {
			get; set;
		}

		public ITaskItem LocalChromiumDir {
			get; set;
		}

		public ITaskItem [] TestSettings_Interfaces {
			get; set;
		}

		public ITaskItem [] ExtraSettings {
			get; set;
		}

		Dictionary<string, string> allSettings = new Dictionary<string, string> ();

		static string Escape (string value) => '"' + EscapingUtilities.Escape (value) + '"';

		public override bool Execute ()
		{
			Log.LogMessage (MessageImportance.High, $"WasmPackager - generate test settings");

			void CheckItem (string name, ITaskItem item)
			{
				if (allSettings.ContainsKey (name)) {
					Log.LogError ($"ExtraSettings contains reserved item '{name}'.");
					return;
				}

				var value = item != null ? Escape (item.ItemSpec) : "null";
				allSettings.Add (name, value);
			}

			if (ExtraSettings != null) {
				foreach (var settings in ExtraSettings) {
					var name = settings.ItemSpec;
					var value = settings.GetMetadata ("Value");

					if (string.IsNullOrWhiteSpace (value)) {
						Log.LogError ($"Missing 'Value' metadata in ExtraSettings item '{name}'.");
						continue;
					}

					if (allSettings.ContainsKey (name)) {
						Log.LogError ($"Dublicate ExtraSettings item '{name}'.");
						continue;
					}

					allSettings.Add (name, Escape (value));
				}
			}

			if (!IsTestSuite) {
				CheckItem ("DevServer_RootDir", DevServer_RootDir);
				CheckItem ("DevServer_FrameworkDir", DevServer_FrameworkDir);
				CheckItem ("DevServer_Arguments", DevServer_Arguments);
				CheckItem ("DevServer_Assembly", DevServer_Assembly);
				CheckItem ("LocalChromiumDir", LocalChromiumDir);
			}

			AutoGenFileHelper.CreateFile (Output.ItemSpec, WriteOutput, Log);

			return true;
		}

		void WriteOutput (TextWriter writer)
		{
			writer.WriteLine ("namespace Mono.WasmPackager.DevServer");
			writer.WriteLine ("{");
			writer.Write ("\tpartial class TestSettings");

			for (int i = 0; i < TestSettings_Interfaces?.Length; i++) {
				writer.Write (i == 0 ? " : " : ", ");
				writer.Write (TestSettings_Interfaces [i].ItemSpec);
			}

			writer.WriteLine ();
			writer.WriteLine ("\t{");

			foreach (var setting in allSettings) {
				writer.WriteLine ($"\t\tpublic string {setting.Key} {{ get; }} = {setting.Value};");
			}

			writer.WriteLine ("\t}");
			writer.WriteLine ("}");
		}
	}
}
