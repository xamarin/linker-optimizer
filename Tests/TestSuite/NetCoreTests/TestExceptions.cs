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

namespace NetCoreTests
{
	public class TestExceptions : PuppeteerTestBase
	{
		protected override bool Headless => true;

		[Fact]
		public async Task TestUnhandled ()
		{
			Assert.Equal (TestConstants.TextReady, await GetInnerHtml ("#output"));

			await SetPauseOnExceptions (PauseOnExceptionMode.All);

			var pause = WaitForPaused ();
			var click = ClickAndWaitForMessage ("#caught", TestConstants.CaughtExceptionText);

			var notification = await pause.ConfigureAwait (false);

			Debug.WriteLine ($"PAUSED: {notification}");

			Assert.Empty (notification.HitBreakpoints);
			Assert.Equal (StoppedReason.Exception, notification.Reason);
			Assert.True (notification.CallFrames.Length >= 2);

			AssertBreakpointFrame (TestSettings.Locations.Throw, notification.CallFrames[0]);
			AssertBreakpointFrame (TestSettings.Locations.CallingThrow, notification.CallFrames[1]);

			Assert.NotNull (notification.Data);
			var exceptionData = notification.Data.ToObject<PausedExceptionData> ();
			Assert.NotNull (exceptionData);

			Assert.Equal ("object", exceptionData.Type);
			Assert.Equal ("error", exceptionData.Subtype);
			Assert.Equal (TestConstants.MyExceptionClassName, exceptionData.ClassName);
			// The message needs to end with a newline or it won't be displayed.
			Assert.Equal (TestConstants.MyExceptionMessage + "\n", exceptionData.Description);
			Assert.StartsWith ("dotnet:exception:", exceptionData.ObjectId);
			Assert.False (exceptionData.Uncaught);

			Debug.WriteLine ("DONE");
		}

		protected async Task SetPauseOnExceptions (PauseOnExceptionMode mode)
		{
			var request = new SetPauseOnExceptionsRequest { State = mode };
			await SendCommand<SetPauseOnExceptionsResponse> ("Debugger.setPauseOnExceptions", request);
		}

	}
}
