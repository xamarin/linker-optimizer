using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Framework;
using Microsoft.Build.Logging;
using Microsoft.Build.Utilities;

namespace TestBuild
{
	class Program
	{
		static Dictionary<string, string> GetGlobalProperties (string projectPath, string toolsPath)
		{
			string solutionDir = Path.GetDirectoryName (projectPath);
			string extensionsPath = Path.GetFullPath (toolsPath);
			string sdksPath = Path.Combine (extensionsPath, "Sdks");
			string roslynTargetsPath = Path.Combine (toolsPath, "Roslyn");

			return new Dictionary<string, string> {
				{ "SolutionDir", solutionDir },
				{ "MSBuildExtensionsPath", extensionsPath },
				{ "MSBuildSDKsPath", sdksPath },
				{ "RoslynTargetsPath", roslynTargetsPath }
			};
		}

		static void Main (string[] args)
		{
			if (args.Length != 1)
				throw new InvalidOperationException ($"Must provide workspace root as argument.");


			var workspaceFolder = args[0];
			var path = Path.Combine (workspaceFolder, Path.GetFileName (workspaceFolder) + ".csproj");

			var toolsPath = "/usr/local/share/dotnet/sdk/3.0.100";
			var globalProperties = GetGlobalProperties (path, toolsPath);

			Environment.SetEnvironmentVariable ("MSBuildExtensionsPath", globalProperties["MSBuildExtensionsPath"]);
			Environment.SetEnvironmentVariable ("MSBuildSDKsPath", globalProperties["MSBuildSDKsPath"]);

			var collection = new ProjectCollection (globalProperties);
			collection.AddToolset (new Toolset (ToolLocationHelper.CurrentToolsVersion, toolsPath, collection, string.Empty));

			StringBuilder logBuilder = new StringBuilder ();
			ConsoleLogger logger = new ConsoleLogger (LoggerVerbosity.Normal, x => {
				logBuilder.Append (x);
				Console.Error.Write (x);
			}, null, null);
			collection.RegisterLogger (logger);

			var project = collection.LoadProject (path);
			var instance = project.CreateProjectInstance ();

			Console.Error.WriteLine ("LOADED PROJECT.");

			if (!instance.Build ("Build", new ILogger[] { logger })) {
				Console.WriteLine ("BUILD FAILED!");
			} else {
				Console.WriteLine ("BUILD SUCCESS!");
			}

			Console.WriteLine (logBuilder);
		}
	}
}
