using System;
using Microsoft.AspNetCore.Builder;

namespace Mono.WasmPackager.DevServer
{
	public class DevServerOptions
	{
		public Uri DevToolsUrl { get; set; } = new Uri ("http://localhost:9222");

		public int DebugServerPort { get; set; } = 9300;

		public int FileServerPort { get; set; } = 8000;

		public string WebRoot { get; set; }

		public FileServerOptions FileServerOptions { get; set; } = new FileServerOptions ();
	}
}
