using System;
using System.Threading.Tasks;
using System.Diagnostics;
using PuppeteerSharp;
using Xunit;

using Mono.WasmPackager.TestSuite;
using Mono.WasmPackager.TestSuite.Messaging.Debugger;
using Mono.WasmPackager.DevServer;

namespace SharedTests
{
	public abstract class TestBreakpoints : PuppeteerTestBase
	{
		public static readonly SourceLocation Breakpoint = TestSettings.Locations.Message;

		async Task AwaitBreakpointHitAndResume (SourceLocation location, Action<PausedNotification> hitAction = null)
		{
			var pause = WaitForPaused ();
			var click = ClickAndWaitForMessage ("#message", TestConstants.MessageText);

			var pausedNotification = await pause.ConfigureAwait (false);

			Assert.NotNull (pausedNotification.HitBreakpoints);
			Assert.Single (pausedNotification.HitBreakpoints);

			Assert.EndsWith (location.File, pausedNotification.HitBreakpoints [0].ToString ());

			hitAction?.Invoke (pausedNotification);

			var resumedNotification = WaitForResumed ();
			var sendResume = Page.Client.SendAsync ("Debugger.resume");

			await Task.WhenAll (resumedNotification, sendResume, click);
		}

		protected async Task SharedInsertHitAndResume ()
		{
			Assert.Equal (TestConstants.TextReady, await GetInnerHtml ("#output"));

			await InsertBreakpoint (Breakpoint);

			await AwaitBreakpointHitAndResume (Breakpoint).ConfigureAwait (false);
		}

		protected async Task SharedInsertRemoveAndResume ()
		{
			Assert.Equal (TestConstants.TextReady, await GetInnerHtml ("#output"));

			var id = await InsertBreakpoint (Breakpoint);
			await RemoveBreakpoint (id);

			await ClickAndWaitForMessage ("#message", TestConstants.MessageText);
		}

		protected async Task SharedInsertRemoveAndInsertAgain ()
		{
			Assert.Equal (TestConstants.TextReady, await GetInnerHtml ("#output"));

			var id = await InsertBreakpoint (Breakpoint);
			await RemoveBreakpoint (id);
			var id2 = await InsertBreakpoint (Breakpoint);
			Assert.Equal (id, id2);

			await AwaitBreakpointHitAndResume (Breakpoint).ConfigureAwait (false);
		}

		protected async Task SharedStackTraceWhenHit ()
		{
			Assert.Equal (TestConstants.TextReady, await GetInnerHtml ("#output"));

			var id = await InsertBreakpoint (Breakpoint);

			await AwaitBreakpointHitAndResume (Breakpoint, notification => {
				AssertBreakpointHit (id, notification);
			}).ConfigureAwait (false);
		}

		protected async Task SharedTestAllFrames ()
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

		protected async Task SharedTestSecondFrame ()
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
