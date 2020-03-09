using System;
using Microsoft.AspNetCore.Builder;

namespace Mono.WasmPackager.DevServer
{
	public class ServerOptions
	{
		public Uri DevToolsUrl { get; set; } = new Uri ("http://localhost:9222");

		public int DebugServerPort { get; set; } = 9300;

		public int FileServerPort { get; set; } = 8000;

		public string WebRoot { get; set; }

		public string FrameworkDirectory { get; set; }

		public bool EnableDebugging { get; set; }

		public bool EnableTestHarness { get; set; }

		public bool Headless { get; set; } = true;
 
		public string PagePath { get; set; } = "index.html";

		public bool VerboseLogging { get; set; } = true;

		public FileServerOptions FileServerOptions { get; set; } = new FileServerOptions ();
	}
}
