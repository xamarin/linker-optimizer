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
using Mono.WasmPackager.TestSuite.Messaging.Debugger;
using Mono.WasmPackager.DevServer;

namespace WorkingTests
{
	public class TestBreakpoints : PuppeteerTestBase
	{
		public static readonly SourceLocation Breakpoint = TestSettings.Locations.Message;

		async Task AwaitBreakpointHitAndResume (SourceLocation location, Action<PausedNotification> hitAction = null)
		{
			var pause = WaitFor (PAUSE);
			var click = ClickAndWaitForMessage ("#message", TestConstants.MessageText);

			var result = await pause.ConfigureAwait (false);
			var hit = result ["hitBreakpoints"] as JArray;
			Assert.NotNull (hit);
			Assert.Single (hit);

			Assert.EndsWith (location.File, hit [0].ToString ());

			if (hitAction != null) {
				var notificationObj = result.ToObject<PausedNotification> (true);
				hitAction (notificationObj);
			}

			var resumedNotification = WaitFor (RESUME);
			var sendResume = Page.Client.SendAsync ("Debugger.resume");

			await Task.WhenAll (resumedNotification, sendResume, click);
		}

		[Fact]
		public async Task InsertHitAndResume ()
		{
			Assert.Equal (TestConstants.TextReady, await GetInnerHtml ("#output"));

			await InsertBreakpoint (Breakpoint);

			await AwaitBreakpointHitAndResume (Breakpoint).ConfigureAwait (false);
		}

		[Fact]
		public async Task InsertRemoveAndResume ()
		{
			Assert.Equal (TestConstants.TextReady, await GetInnerHtml ("#output"));

			var id = await InsertBreakpoint (Breakpoint);
			await RemoveBreakpoint (id);

			await ClickAndWaitForMessage ("#message", TestConstants.MessageText);
		}

		[Fact]
		public async Task InsertRemoveAndInsertAgain ()
		{
			Assert.Equal (TestConstants.TextReady, await GetInnerHtml ("#output"));

			var id = await InsertBreakpoint (Breakpoint);
			await RemoveBreakpoint (id);
			var id2 = await InsertBreakpoint (Breakpoint);
			Assert.Equal (id, id2);

			await AwaitBreakpointHitAndResume (Breakpoint).ConfigureAwait (false);
		}

		[Fact]
		public async Task StackTraceWhenHit ()
		{
			Assert.Equal (TestConstants.TextReady, await GetInnerHtml ("#output"));

			var id = await InsertBreakpoint (Breakpoint);

			await AwaitBreakpointHitAndResume (Breakpoint, notification => {
				AssertBreakpointHit (id, notification);
			}).ConfigureAwait (false);
		}

		[Fact]
		public async Task TestAllFrames ()
		{
			Assert.Equal (TestConstants.TextReady, await GetInnerHtml ("#output"));

			var id = await InsertBreakpoint (Breakpoint);

			await AwaitBreakpointHitAndResume (Breakpoint, notification => {
				AssertBreakpointHit (id, notification);
				foreach (var frame in notification.CallFrames) {
					Assert.NotNull (frame.Location);
				}
			}).ConfigureAwait (false);
		}

		[Fact]
		public async Task TestSecondFrame ()
		{
			Assert.Equal (TestConstants.TextReady, await GetInnerHtml ("#output"));

			var id = await InsertBreakpoint (Breakpoint);

			await AwaitBreakpointHitAndResume (Breakpoint, notification => {
				AssertBreakpointHit (id, notification);
				var second = notification.CallFrames [1];
				Debug.WriteLine ($"SECOND FRAME: {second}");
			}).ConfigureAwait (false);
		}
	}
}
