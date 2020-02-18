using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using System.Diagnostics;

using System.Collections.Generic;

using WebAssembly.Net.Debugging;
using Mono.WasmPackager.DevServer;
using PuppeteerSharp;
using Xunit;

namespace Mono.WasmPackager.TestSuite
{
	public static class TestSuiteSetup
	{
		static Task initTask;
		static BrowserFetcher browserFetcher;
		static RevisionInfo revisionInfo;

		public static Task Initialize ()
		{
			return LazyInitializer.EnsureInitialized (ref initTask, async () =>
			{
				var localPath = GetLocalChromiumDirectory ();
				if (localPath == null)
					throw new InvalidOperationException ("Failed to get local chromium path.");
				var options = new BrowserFetcherOptions { Path = localPath };
				browserFetcher = Puppeteer.CreateBrowserFetcher (options);
				revisionInfo = await browserFetcher.DownloadAsync (BrowserFetcher.DefaultRevision);
			});
		}

		static string GetLocalChromiumDirectory ()
		{
			var asm = Assembly.GetExecutingAssembly ();
			var root = asm.Location;
			root = Path.GetDirectoryName (root);
			if (Path.GetFileName (root) != "netcoreapp3.0")
				return null;
			root = Path.GetDirectoryName (root);
			if (Path.GetFileName (root) != "Debug")
				return null;
			root = Path.GetDirectoryName (root);
			if (Path.GetFileName (root) != "bin")
				return null;
			root = Path.GetDirectoryName (root);
			root = Path.GetDirectoryName (root);
			if (Path.GetFileName (root) != "Tests")
				return null;
			root = Path.GetDirectoryName (root);
			if (Path.GetFileName (root) != "Packager")
				return null;
			root = Path.Combine (root, ".local-chromium");
			Debug.WriteLine ($"Using local chromium {root}.");
			return root;
		}

		internal static string ChromiumPath => revisionInfo.ExecutablePath;

		public static async Task<string> GetChromiumPath ()
		{
			await Initialize ().ConfigureAwait (false);
			return ChromiumPath;
		}
	}
}
