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
		async Task AwaitBreakpointHitAndResume (Action<PausedNotification> hitAction = null)
		{
			var pause = WaitFor (PAUSE);
			var click = ClickAndWaitForMessage ("#message", TestConstants.MessageText);

			var result = await pause.ConfigureAwait (false);
			var hit = result ["hitBreakpoints"] as JArray;
			Assert.NotNull (hit);
			Assert.Single (hit);

			Assert.EndsWith (TestConstants.HelloFile, hit [0].ToString ());

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

			await InsertBreakpoint (TestConstants.MessageBreakpoint);

			await AwaitBreakpointHitAndResume ().ConfigureAwait (false);
		}

		[Fact]
		public async Task InsertRemoveAndResume ()
		{
			Assert.Equal (TestConstants.TextReady, await GetInnerHtml ("#output"));

			var id = await InsertBreakpoint (TestConstants.MessageBreakpoint);
			await RemoveBreakpoint (id);

			await ClickAndWaitForMessage ("#message", TestConstants.MessageText);
		}

		[Fact]
		public async Task InsertRemoveAndInsertAgain ()
		{
			Assert.Equal (TestConstants.TextReady, await GetInnerHtml ("#output"));

			var id = await InsertBreakpoint (TestConstants.MessageBreakpoint);
			await RemoveBreakpoint (id);
			var id2 = await InsertBreakpoint (TestConstants.MessageBreakpoint);
			Assert.Equal (id, id2);

			await AwaitBreakpointHitAndResume ().ConfigureAwait (false);
		}

		void AssertBreakpointHit (string id, PausedNotification notification)
		{
			Assert.Single (notification.HitBreakpoints);
			Assert.Equal (id, notification.HitBreakpoints [0]);
			Assert.Equal (StoppedReason.Other, notification.Reason);
			Assert.True (notification.CallFrames.Length > 0);
			AssertBreakpointFrame (TestConstants.MessageBreakpoint, notification.CallFrames [0]);
		}

		[Fact]
		public async Task StackTraceWhenHit ()
		{
			Assert.Equal (TestConstants.TextReady, await GetInnerHtml ("#output"));

			var id = await InsertBreakpoint (TestConstants.MessageBreakpoint);

			await AwaitBreakpointHitAndResume (notification => {
				AssertBreakpointHit (id, notification);
			}).ConfigureAwait (false);
		}

		[Fact]
		public async Task TestAllFrames ()
		{
			Assert.Equal (TestConstants.TextReady, await GetInnerHtml ("#output"));

			var id = await InsertBreakpoint (TestConstants.MessageBreakpoint);

			await AwaitBreakpointHitAndResume (notification => {
				AssertBreakpointHit (id, notification);
				foreach (var frame in notification.CallFrames) {
					Assert.NotNull (frame.Location);
				}
			}).ConfigureAwait (false);
		}
	}
}
