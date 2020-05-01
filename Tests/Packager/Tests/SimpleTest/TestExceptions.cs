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

namespace SimpleTest
{
	public class TestExceptions : PuppeteerTestBase
	{
		protected override bool Headless => true;

		[Fact]
		public async Task TestUnhandled ()
		{
			Assert.Equal (TestConstants.TextReady, await GetInnerHtml ("#output"));

			await SetPauseOnExceptions (PauseOnExceptionMode.All);

			var pause = WaitFor (PAUSE);
			var click = ClickAndWaitForMessage ("#caught", TestConstants.CaughtExceptionText);

			var result = await pause.ConfigureAwait (false);
			var notification = result.ToObject<PausedNotification> (true);

			Debug.WriteLine ($"PAUSED: {notification}");

			Assert.Empty (notification.HitBreakpoints);
			Assert.Equal (StoppedReason.Other, notification.Reason);
			Assert.True (notification.CallFrames.Length >= 2);

			AssertBreakpointFrame (TestConstants.ThrowMethod, notification.CallFrames[0]);
			AssertBreakpointFrame (TestConstants.ThrownLocation, notification.CallFrames[1]);

			Debug.WriteLine ("DONE");
		}

		protected async Task SetPauseOnExceptions (PauseOnExceptionMode mode)
		{
			var request = new SetPauseOnExceptionsRequest { State = mode };
			await SendCommand<SetPauseOnExceptionsResponse> ("Debugger.setPauseOnExceptions", request);
		}

	}
}
