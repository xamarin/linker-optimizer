using System;
using System.Threading;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using PuppeteerSharp;
using Xunit;

using Mono.WasmPackager.TestSuite;
using Mono.WasmPackager.DevServer;

namespace SimpleTest
{
	public class PuppeteerInspector : PuppeteerTestBase
	{
		[Fact]
		public async Task TestMessage ()
		{
			Debug.WriteLine ($"SERVER READY: {Page}");

			var selector = await Page.QuerySelectorAsync ("#output");
			Debug.WriteLine ($"SELECTOR: {selector}");

			var inner = await GetInnerHtml ("#output");

			Debug.WriteLine ($"INNER: {inner}");

			Assert.Equal (TextReady, await GetInnerHtml ("#output"));
			await ClickAndWaitForMessage ("#message", MessageText2);
			Assert.Equal (TextMessage, await GetInnerHtml ("#output"));
		}

		[Fact]
		public async Task TestSimpleMessage ()
		{
			Assert.Equal (TextReady, await GetInnerHtml ("#output"));
			await ClickAndWaitForMessage ("#message", MessageText2);
			Assert.Equal (TextMessage, await GetInnerHtml ("#output"));
		}

		[Fact]
		public async Task TestThrow ()
		{
			Assert.Equal (TextReady, await GetInnerHtml ("#output"));
			var exception = await ClickAndWaitForException ("#throw", "System.InvalidOperationException");
			Debug.WriteLine ($"GOT EXCEPTION: |{exception}|");
			Assert.Equal (ThrowMessage, await GetInnerHtml ("#output"));
		}

		[Fact]
		public async Task TestBreakpoint ()
		{
			Assert.Equal (TextReady, await GetInnerHtml ("#output"));
			await GetPossibleBreakpoints ("Hello.cs");
			Debug.WriteLine ($"TEST DONE!");
		}

		[Fact]
		public async Task TestBreakpoint2 ()
		{
			var file = "Hello.cs";
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
	}
}
