using System;
using System.Threading.Tasks;
using System.Collections.Generic;
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
			var click = ClickAndWaitForMessage (TestConstants.Selectors.Message, TestConstants.MessageText);

			var pausedNotification = await pause.ConfigureAwait (false);

			Assert.NotNull (pausedNotification.HitBreakpoints);
			Assert.Single (pausedNotification.HitBreakpoints);

			Assert.EndsWith (location.File, pausedNotification.HitBreakpoints [0].ToString ());

			hitAction?.Invoke (pausedNotification);

			var resumedNotification = WaitForResumed ();
			var sendResume = Resume ();

			await Task.WhenAll (resumedNotification, sendResume, click);
		}

		protected async Task SharedInsertHitAndResume ()
		{
			Assert.Equal (TestConstants.TextReady, await GetInnerHtml (TestConstants.Selectors.Output));

			await InsertBreakpoint (Breakpoint);

			await AwaitBreakpointHitAndResume (Breakpoint).ConfigureAwait (false);

			Debug.WriteLine ($"DONE");
		}

		protected async Task SharedInsertRemoveAndResume ()
		{
			Assert.Equal (TestConstants.TextReady, await GetInnerHtml (TestConstants.Selectors.Output));

			var id = await InsertBreakpoint (Breakpoint);
			await RemoveBreakpoint (id);

			await ClickAndWaitForMessage (TestConstants.Selectors.Message, TestConstants.MessageText);
		}

		protected async Task SharedInsertRemoveAndInsertAgain ()
		{
			Assert.Equal (TestConstants.TextReady, await GetInnerHtml (TestConstants.Selectors.Output));

			var binfo = await InsertBreakpoint (Breakpoint);
			await RemoveBreakpoint (binfo);
			var binfo2 = await InsertBreakpoint (Breakpoint);
			Assert.Equal (binfo.Id, binfo2.Id);

			await AwaitBreakpointHitAndResume (Breakpoint).ConfigureAwait (false);
		}

		protected async Task SharedStackTraceWhenHit ()
		{
			Assert.Equal (TestConstants.TextReady, await GetInnerHtml (TestConstants.Selectors.Output));

			var id = await InsertBreakpoint (Breakpoint);

			await AwaitBreakpointHitAndResume (Breakpoint, notification => {
				AssertBreakpointHit (id, notification);
			}).ConfigureAwait (false);
		}

		protected async Task SharedTestAllFrames ()
		{
			Assert.Equal (TestConstants.TextReady, await GetInnerHtml (TestConstants.Selectors.Output));

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
			Assert.Equal (TestConstants.TextReady, await GetInnerHtml (TestConstants.Selectors.Output));

			var id = await InsertBreakpoint (Breakpoint);

			await AwaitBreakpointHitAndResume (Breakpoint, notification => {
				AssertBreakpointHit (id, notification);
				var second = notification.CallFrames [1];
				Debug.WriteLine ($"SECOND FRAME: {second}");
			}).ConfigureAwait (false);
		}

		string PrintCallFrame (CallFrame frame)
		{
			return $"[{frame.FunctionName} - {frame.Location.LineNumber}:{frame.Location.ColumnNumber} - {frame.Url}";
		}

		protected async Task SharedStepOver ()
		{
			Assert.Equal (TestConstants.TextReady, await GetInnerHtml (TestConstants.Selectors.Output));

			var id = await InsertBreakpoint (TestSettings.Locations.StepOverFirstLine);
			Debug.WriteLine ($"FIRST LINE: {TestSettings.Locations.StepOverFirstLine}");
			Debug.WriteLine ($"SECOND LINE LINE: {TestSettings.Locations.StepOverSecondLine}");

			await ClickWithPausedNotification (
				TestConstants.Selectors.StepOver,
				async notification => {
					Debug.WriteLine ($"FIRST STOP: {PrintCallFrame (notification.CallFrames[0])}");

					AssertBreakpointHit (id, notification);

					var waitForResumed = WaitForResumed ();
					var waitForPaused = WaitForPaused ();

					await StepOver ().ConfigureAwait (false);

					await Task.WhenAll (waitForResumed, waitForPaused).ConfigureAwait (false);

					var secondStop = waitForPaused.Result;
					Debug.WriteLine ($"SECOND STOP: {PrintCallFrame (secondStop.CallFrames[0])}");

					AssertBreakpointFrame (TestSettings.Locations.StepOverSecondLine, secondStop.CallFrames[0]);
				}).ConfigureAwait (false);

			Debug.WriteLine ($"DONE");
		}

		protected async Task SharedJsBreakpoint ()
		{
			Assert.Equal (TestConstants.TextReady, await GetInnerHtml (TestConstants.Selectors.Output));

			var id = await InsertBreakpoint (TestSettings.Locations.JsVariables);

			await ClickWithPausedNotification (
				TestConstants.Selectors.JsVariables,
				async notification => {
					AssertBreakpointHit (id, notification);
					await Task.CompletedTask;
				});
		}
	}
}
