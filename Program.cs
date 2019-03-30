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
using System.Xml.XPath;
using System.Collections.Generic;
using Mono.Linker.Steps;

namespace Mono.Linker.Optimizer
{
	public static class Program
	{
		static readonly MartinOptions options = new MartinOptions ();

		public static int Main (string[] args)
		{
			if (args.Length == 0) {
				Console.Error.WriteLine ("No parameters specified");
				return 1;
			}

			var arguments = ProcessResponseFile (args);
			ParseArguments (arguments);

			var env = Environment.GetEnvironmentVariable ("MARTIN_LINKER_OPTIONS");
			if (!string.IsNullOrEmpty (env))
				options.ParseOptions (env);

			Driver.Execute (arguments.ToArray ());

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
			var martinsPlayground = false;

			while (arguments.Count > 0) {
				var token = arguments[0];
				if (!token.StartsWith ("--martin", StringComparison.Ordinal))
					break;

				arguments.RemoveAt (0);
				switch (token) {
				case "--martin":
					martinsPlayground = true;
					continue;
				case "--martin-xml":
					var filename = arguments[0];
					arguments.RemoveAt (0);
					OptionsReader.Read (options, filename);
					martinsPlayground = true;
					break;
				case "--martin-args":
					options.ParseOptions (arguments[0]);
					arguments.RemoveAt (0);
					martinsPlayground = true;
					break;
				}
			}

			if (!martinsPlayground)
				return;

			arguments.Insert (0, "--custom-step");
			arguments.Insert (1, $"TypeMapStep:{typeof (InitializeStep).AssemblyQualifiedName}");
		}

		class InitializeStep : IStep
		{
			public void Process (LinkContext context)
			{
				MartinContext.Initialize (context, options);
			}
		}
	}
}

