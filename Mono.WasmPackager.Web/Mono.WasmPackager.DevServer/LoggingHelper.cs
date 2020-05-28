using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using WebAssembly.Net.Debugging;

namespace Mono.WasmPackager.DevServer
{
	public static class LoggingHelper
	{
		static bool DontLogArgs (string method)
		{
			switch (method) {
			case "Runtime.consoleAPICalled":
			case "Debugger.getWasmBytecode":
			case "Debugger.scriptParsed":
				return true;
			default:
				return false;
			}
		}

		public static void LogProtocol (object sender, string method, string msg, object args = null)
		{
			if (args == null || DontLogArgs (method))
				Debug.WriteLine ($"[{sender.GetType ().Name}]: {msg} {method}");
			else
				Debug.WriteLine ($"[{sender.GetType ().Name}]: {msg} {method} {args}");
		}
	}
}
