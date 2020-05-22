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
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Mono.Linker.Steps;

namespace Mono.Linker.Optimizer
{
	public class Program : Driver
	{
		static readonly OptimizerOptions options = new OptimizerOptions ();
		static string mainModule;
		static bool moduleEnabled;

		internal const string ProgramName = "Mono Linker Optimizer";

		new public static int Main (string [] args)
		{
			if (args.Length == 0) {
				Console.Error.WriteLine ("No parameters specified");
				return 1;
			}

			if (!ProcessResponseFile (args, out var arguments))
				return 1;

			ParseArguments (arguments);

			var env = Environment.GetEnvironmentVariable ("LINKER_OPTIMIZER_OPTIONS");
			if (!string.IsNullOrEmpty (env)) {
				moduleEnabled = true;
				options.ParseOptions (env);
			}

			moduleEnabled &= !options.DisableModule;

			if (moduleEnabled && mainModule == null) {
				Console.Error.WriteLine ("Missing main module argument.");
				return 1;
			}

			if (moduleEnabled) {
				if (!options.IsFeatureEnabled (MonoLinkerFeature.ReflectionEmit)) {
					arguments.Enqueue ("--exclude-feature");
					arguments.Enqueue ("sre");
				}
				if (!options.IsFeatureEnabled (MonoLinkerFeature.Security)) {
					arguments.Enqueue ("--exclude-feature");
					arguments.Enqueue ("security");
				}
				if (!options.IsFeatureEnabled (MonoLinkerFeature.Remoting)) {
					arguments.Enqueue ("--exclude-feature");
					arguments.Enqueue ("remoting");
				}
				if (!options.IsFeatureEnabled (MonoLinkerFeature.Globalization)) {
					arguments.Enqueue ("--exclude-feature");
					arguments.Enqueue ("globalization");
				}
				if (mainModule != null) {
					arguments.Enqueue ("-a");
					arguments.Enqueue (mainModule);
				}
			} else {
				Console.Error.WriteLine ($"Optimizer is disabled.");
			}

			// Always disable the RemoveUnreachableBlocksStep; it is incomplete and does not work.
			arguments.Enqueue ("--disable-opt");
			arguments.Enqueue ("ipconstprop");

			var watch = new Stopwatch ();
			watch.Start ();

			try {
				var driver = new Program (arguments);
				driver.Run ();
			} catch (OptimizerException ex) {
				Console.Error.WriteLine ($"Fatal error in {ProgramName}: {ex.Message}");
				Console.Error.WriteLine ();
				return 1;
			} catch (MarkException ex) {
				if (ex.InnerException is OptimizerException optimizerException) {
					Console.Error.WriteLine ($"Fatal error in {ProgramName}: {optimizerException.Message}");
					Console.Error.WriteLine ();
					return 1;
				}
				throw;
			} catch (Exception ex) {
				Console.Error.WriteLine ($"Fatal error in {ProgramName}: {ex.Message}");
				Console.Error.WriteLine ();
				throw;
			}

			watch.Stop ();

			Console.Error.WriteLine ($"{ProgramName} finished in {watch.Elapsed}.");
			Console.Error.WriteLine ();

			return 0;
		}

		public Program (Queue<string> arguments)
			: base (arguments)
		{
		}

		protected override LinkContext GetDefaultContext (Pipeline pipeline)
		{
			if (moduleEnabled) {
				var step = new InitializeStep (mainModule);
				pipeline.PrependStep (step);
			}
			return base.GetDefaultContext (pipeline);
		}

		static void ParseArguments (Queue<string> arguments)
		{
			while (arguments.Count > 0) {
				var token = arguments.Peek ();
				if (token == "--") {
					arguments.Dequeue ();
					break;
				}
				if (!token.StartsWith ("--optimizer", StringComparison.Ordinal))
					break;

				arguments.Dequeue ();
				switch (token) {
				case "--optimizer":
					if (mainModule != null) {
						Console.Error.WriteLine ($"Duplicate --optimizer argument.");
						Environment.Exit (1);
					}
					mainModule = arguments.Dequeue ();
					LoadFile (mainModule);
					moduleEnabled = true;
					continue;
				case "--optimizer-xml":
					var filename = arguments.Dequeue ();
					OptionsReader.Read (options, filename);
					moduleEnabled = true;
					break;
				case "--optimizer-options":
					options.ParseOptions (arguments.Dequeue ());
					moduleEnabled = true;
					break;
				case "--optimizer-report":
					filename = arguments.Dequeue ();
					options.ReportFileName = filename;
					break;
				case "--optimizer-ref":
					filename = arguments.Dequeue ();
					options.AssemblyReferences.Add (filename);
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
			public string MainModule {
				get;
			}

			public InitializeStep (string mainModule)
			{
				MainModule = mainModule;
			}

			public void Process (LinkContext context)
			{
				OptimizerContext.Initialize (context, MainModule, options);
			}
		}
	}
}

