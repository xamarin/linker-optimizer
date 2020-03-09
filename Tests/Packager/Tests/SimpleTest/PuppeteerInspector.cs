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
		// Keep in sync with the javascript side
		const string MessageText = "MESSAGE BUTTON CLICKED";
		const string MessageText2 = "MESSAGE BUTTON CLICKED - BACK FROM MANAGED";
		const string TextReady = "READY";
		const string TextMessage = "MESSAGE";
		const string ThrowMessage = "THROW";

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

	}
}
