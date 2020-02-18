using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

using System.Net.WebSockets;
using System.Collections.Generic;

using WebAssembly.Net.Debugging;
using Mono.WasmPackager.DevServer;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Mono.WasmPackager.TestSuite
{
	public abstract class DebuggerTestBase : IAsyncLifetime, IDisposable
	{
		Task serverTask;
		bool disposed;

		static string[] PROBE_LIST = {
			"/Applications/Google Chrome Canary.app/Contents/MacOS/Google Chrome Canary",
			"/Applications/Google Chrome.app/Contents/MacOS/Google Chrome",
			"/usr/bin/chromium",
			"/usr/bin/chromium-browser",
		};
		static string chrome_path;
		static string FindChromePath ()
		{
			if (chrome_path != null)
				return chrome_path;

			foreach (var s in PROBE_LIST) {
				if (File.Exists (s)) {
					chrome_path = s;
					Debug.WriteLine ($"Using chrome path: {s}");
					return s;
				}
			}
			throw new Exception ("Could not find an installed Chrome to use");
		}

		public ITestSuiteSettings Settings {
			get;
		}

		protected Server Server {
			get;
		}

		protected DebuggerTestBase (Assembly caller = null)
		{
			if (caller == null)
				caller = Assembly.GetCallingAssembly ();
			var type = caller.GetType ("Mono.WasmPackager.DevServer.TestSettings");
			Settings = (ITestSuiteSettings)Activator.CreateInstance (type, BindingFlags.Public | BindingFlags.Instance, null, null, null);
			if (Settings == null)
				throw new InvalidOperationException ("Unable to resolve test settings.");

			var chrome = FindChromePath ();
			Debug.WriteLine ($"DebuggerTestBase: {chrome} {Settings.DevServer_RootDir}");
			Server = Server.CreateTestHarness (Settings.DevServer_RootDir, chrome);
			serverTask = Server.Host.StartAsync ();
		}

		public virtual Task InitializeAsync () => serverTask;

		public virtual Task DisposeAsync ()
		{
			Server.Dispose ();
			return Task.CompletedTask;
		}

		void IDisposable.Dispose ()
		{
			lock (this) {
				if (disposed)
					return;
				disposed = true;
			}

			Server.Dispose ();
		}
	}
}
