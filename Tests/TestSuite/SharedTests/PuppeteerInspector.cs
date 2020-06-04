using System;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using PuppeteerSharp;
using Xunit;

using Mono.WasmPackager.TestSuite;
using Mono.WasmPackager.DevServer;

namespace SharedTests
{
	public abstract class PuppeteerInspector : PuppeteerTestBase
	{
		protected async Task SharedTestMessage ()
		{
			Debug.WriteLine ($"SERVER READY: {Page}");

			var selector = await Page.QuerySelectorAsync ("#output");
			Debug.WriteLine ($"SELECTOR: {selector}");

			var inner = await GetInnerHtml ("#output");

			Debug.WriteLine ($"INNER: {inner}");

			Assert.Equal (TestConstants.TextReady, await GetInnerHtml ("#output"));
			await ClickAndWaitForMessage ("#message", TestConstants.MessageText2);
			Assert.Equal (TestConstants.TextMessage, await GetInnerHtml ("#output"));
		}

		protected async Task SharedTestSimpleMessage ()
		{
			Assert.Equal (TestConstants.TextReady, await GetInnerHtml ("#output"));
			await ClickAndWaitForMessage ("#message", TestConstants.MessageText2);
			Assert.Equal (TestConstants.TextMessage, await GetInnerHtml ("#output"));
		}

		protected async Task SharedTestThrow ()
		{
			Assert.Equal (TestConstants.TextReady, await GetInnerHtml ("#output"));
			var exception = await ClickAndWaitForException ("#throw", "System.InvalidOperationException");
			Debug.WriteLine ($"GOT EXCEPTION: |{exception}|");
			Assert.Equal (TestConstants.ThrowMessage, await GetInnerHtml ("#output"));
		}

		protected async Task SharedTestBreakpoint ()
		{
			Assert.Equal (TestConstants.TextReady, await GetInnerHtml ("#output"));
			await GetPossibleBreakpoints (TestSettings.Locations.Message.File);
			Debug.WriteLine ($"TEST DONE!");
		}

		protected async Task SharedTestBreakpoint2 ()
		{
			var file = TestSettings.Locations.Message.File;
			var id = FileToId [$"dotnet://{Settings.DevServer_Assembly}/{file}"];
			var bp1_req = JObject.FromObject (new {
				start = JObject.FromObject (new {
					scriptId = id,
					lineNumber = 0,
					columnNumber = 0
				}),
				end = JObject.FromObject (new {
					scriptId = id + 1,
					lineNumber = 0,
					columnNumber = 0
				})
			});

			try {
				var bp1_res = await Session.Session.SendAsync ("Debugger.getPossibleBreakpoints", bp1_req);
				Debug.WriteLine ($"TEST DONE: {bp1_res}");
			} catch (MessageException ex) {
				Debug.WriteLine ($"GOT EXCEPTION: {ex.Message}");
			}
		}

		protected async Task SharedGetVersion (Version expected)
		{
			Debug.WriteLine ($"GET VERSION: {expected}");

			var version = await GetInnerHtml ("#version");
			var osversion = await GetInnerHtml ("#osversion");
			Assert.NotNull (version);
			Assert.NotNull (osversion);

			Debug.WriteLine ($"GOT VERSION: {version} {osversion}");

			var versionObj = Version.Parse (version);
			Assert.Equal (expected.Major, versionObj.Major);
			Assert.Equal (expected.Minor, versionObj.Minor);
		}
	}
}
