using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

using System.Collections.Generic;

using WebAssembly.Net.Debugging;
using Mono.WasmPackager.DevServer;
using PuppeteerSharp;
using Xunit;

namespace Mono.WasmPackager.TestSuite
{
	public static class BrowserController
	{
		const int DevToolsPort = 9022;

		public static async Task Connect (string url)
		{
			var chromiumPath = await TestSuiteSetup.GetChromiumPath ();

			var options = new LaunchOptions ()
			{
				Headless = true,
				Args = new[] {
					"--disable-gpu",
					$"--remote-debugging-port=0"
				},
				IgnoreDefaultArgs = false,
				ExecutablePath = chromiumPath
			};

			using (var browser = await PuppeteerSharp.Puppeteer.LaunchAsync (options)) {
				var context = await browser.CreateIncognitoBrowserContextAsync ();
				using (var page = await context.NewPageAsync ()) {
					await page.GoToAsync (url);
				}
			}
		}
	}
}
