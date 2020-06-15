using System;
using System.Reflection;
using System.Threading.Tasks;
using System.Diagnostics;

using Mono.WasmPackager.DevServer;
using Xunit;

namespace Mono.WasmPackager.TestSuite
{
	public abstract class DebuggerTestBase : IAsyncLifetime, IDisposable
	{
		readonly Task serverTask;
		bool disposed;

		public ITestSuiteSettings Settings {
			get;
		}

		public Uri ServerRoot {
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

			Debug.WriteLine ($"DevServer_RootDir: {Settings.DevServer_RootDir}");
			Debug.WriteLine ($"DevServer_FrameworkDir: {Settings.DevServer_FrameworkDir}");

			ServerRoot = new Uri ("http://localhost:8000/");

			Server = Server.CreateTestHarness (Settings.DevServer_RootDir, Settings.DevServer_FrameworkDir);
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
