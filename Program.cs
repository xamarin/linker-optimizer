//
// Program.cs
//
// Author:
//       Martin Baulig <mabaul@microsoft.com>
//
// Copyright (c) 2019 Microsoft Corporation
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Mono.Linker.Steps;
using System.Diagnostics;

namespace Mono.Linker.Optimizer
{
	public static class Program
	{
		static readonly OptimizerOptions options = new OptimizerOptions ();
		static string mainModule;
		static bool moduleEnabled;

		const string ProgramName = "Mono Linker Optimizer";

		public static int Main (string[] args)
		{
			if (args.Length == 0) {
				Console.Error.WriteLine ("No parameters specified");
				return 1;
			}

			var arguments = ProcessResponseFile (args);
			ParseArguments (arguments);

			var env = Environment.GetEnvironmentVariable ("MARTIN_LINKER_OPTIONS");
			if (!string.IsNullOrEmpty (env)) {
				moduleEnabled = true;
				options.ParseOptions (env);
			}

			moduleEnabled &= !options.DisableModule;

			if (moduleEnabled) {
				if (mainModule == null) {
					Console.Error.WriteLine ("Missing main module argument.");
					return 1;
				}
				arguments.Insert (0, "-a");
				arguments.Insert (1, mainModule);
				arguments.Insert (2, "--custom-step");
				arguments.Insert (3, $"TypeMapStep:{typeof (InitializeStep).AssemblyQualifiedName}");
			}

			var watch = new Stopwatch ();
			watch.Start ();

			try {
				var driver = new Driver (arguments.ToArray ());
				driver.Run ();
			} catch (Exception ex) {
				Console.Error.WriteLine ($"Fatal error in {ProgramName}: {ex.Message}");
				throw;
			}

			watch.Stop ();

			Console.Error.WriteLine ($"{ProgramName} finished in {watch.Elapsed}.");

			return 0;
		}

		static List<string> ProcessResponseFile (string[] args)
		{
			var result = new Queue<string> ();
			foreach (string arg in args) {
				if (arg.StartsWith ("@", StringComparison.Ordinal)) {
					try {
						var responseFileName = arg.Substring (1);
						var responseFileLines = File.ReadLines (responseFileName);
						Driver.ParseResponseFileLines (responseFileLines, result);
					} catch (Exception e) {
						Console.Error.WriteLine ("Cannot read response file with exception " + e.Message);
						Environment.Exit (1);
					}
				} else {
					result.Enqueue (arg);
				}
			}
			return result.ToList ();
		}

		static void ParseArguments (List<string> arguments)
		{
			while (arguments.Count > 0) {
				var token = arguments[0];
				if (!token.StartsWith ("--martin", StringComparison.Ordinal))
					break;

				arguments.RemoveAt (0);
				switch (token) {
				case "--martin":
					if (mainModule != null) {
						Console.Error.WriteLine ($"Duplicate --martin argument.");
						Environment.Exit (1);
					}
					mainModule = arguments[0];
					arguments.RemoveAt (0);
					LoadFile (mainModule);
					moduleEnabled = true;
					continue;
				case "--martin-xml":
					var filename = arguments[0];
					arguments.RemoveAt (0);
					OptionsReader.Read (options, filename);
					moduleEnabled = true;
					break;
				case "--martin-options":
					options.ParseOptions (arguments[0]);
					arguments.RemoveAt (0);
					moduleEnabled = true;
					break;
				}
			}
		}

		static void LoadFile (string filename)
		{
			var xml = Path.ChangeExtension (Path.GetFileNameWithoutExtension (filename), "xml");
			if (File.Exists (xml))
				OptionsReader.Read (options, xml);
		}

		class InitializeStep : IStep
		{
			public void Process (LinkContext context)
			{
				OptimizerContext.Initialize (context, options);
			}
		}
	}
}

