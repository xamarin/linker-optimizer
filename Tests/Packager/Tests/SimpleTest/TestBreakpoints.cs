using System;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using PuppeteerSharp;
using Xunit;

using Mono.WasmPackager.TestSuite;
using Mono.WasmPackager.DevServer;

namespace SimpleTest
{
	public class TestBreakpoints : PuppeteerTestBase
	{
		protected override bool Headless => false;

		async Task AwaitBreakpointHitAndResume ()
		{
			var pause = WaitFor (PAUSE);
			var click = ClickAndWaitForMessage ("#message", MessageText);

			var result = await pause.ConfigureAwait (false);
			var hit = result ["hitBreakpoints"] as JArray;
			Assert.NotNull (hit);
			Assert.Single (hit);

			Assert.EndsWith (TestSource.File, hit [0].ToString ());

			var resumedNotification = WaitFor (RESUME);
			var sendResume = Page.Client.SendAsync ("Debugger.resume");

			await Task.WhenAll (resumedNotification, sendResume, click);
		}

		[Fact]
		public async Task InsertHitAndResume ()
		{
			Assert.Equal (TextReady, await GetInnerHtml ("#output"));

			await InsertBreakpoint (TestSource.File, TestSource.MessageBreakpoint);

			await AwaitBreakpointHitAndResume ().ConfigureAwait (false);
		}

		[Fact]
		public async Task InsertRemoveAndResume ()
		{
			Assert.Equal (TextReady, await GetInnerHtml ("#output"));

			var id = await InsertBreakpoint (TestSource.File, TestSource.MessageBreakpoint);
			await RemoveBreakpoint (id);

			await ClickAndWaitForMessage ("#message", MessageText);
		}

		[Fact]
		public async Task InsertRemoveAndInsertAgain ()
		{
			Assert.Equal (TextReady, await GetInnerHtml ("#output"));

			var id = await InsertBreakpoint (TestSource.File, TestSource.MessageBreakpoint);
			await RemoveBreakpoint (id);
			var id2 = await InsertBreakpoint (TestSource.File, TestSource.MessageBreakpoint);
			Assert.Equal (id, id2);

			await AwaitBreakpointHitAndResume ().ConfigureAwait (false);
		}

	}
}
