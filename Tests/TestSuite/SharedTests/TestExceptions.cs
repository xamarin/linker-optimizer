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
using Mono.WasmPackager.TestSuite.Messaging.Runtime;
using Mono.WasmPackager.DevServer;

namespace SharedTests
{
	public abstract class TestExceptions : PuppeteerTestBase
	{
		protected override bool Headless => true;

		protected TimeSpan DefaultTimeout = TimeSpan.FromSeconds (15);

		protected async Task SharedTestUnhandled ()
		{
			Assert.Equal (TestConstants.TextReady, await GetInnerHtml ("#output"));

			await SetPauseOnExceptions (PauseOnExceptionMode.All);

			var pause = WaitForPaused ();
			var click = ClickAndWaitForMessage ("#caught", TestConstants.CaughtExceptionText);
			var timeout = Task.Delay (DefaultTimeout);

			var result = await Task.WhenAny (pause, click, timeout).ConfigureAwait (false);
			Debug.WriteLine ($"RESULT: {result}");

			Assert.True (timeout.Status == TaskStatus.WaitingForActivation);
			Assert.Equal (TaskStatus.WaitingForActivation, click.Status);
			Assert.Equal (TaskStatus.RanToCompletion, pause.Status);

			var notification = pause.Result;

			Debug.WriteLine ($"PAUSED: {notification}");

			Assert.Empty (notification.HitBreakpoints);
			Assert.Equal (StoppedReason.Exception, notification.Reason);
			Assert.True (notification.CallFrames.Length >= 2);

			AssertBreakpointFrame (TestSettings.Locations.Throw, notification.CallFrames[0]);
			AssertBreakpointFrame (TestSettings.Locations.CallingThrow, notification.CallFrames[1]);

			Assert.NotNull (notification.Data);
			var exceptionData = notification.Data.ToObject<PausedExceptionData> ();
			Assert.NotNull (exceptionData);

			Assert.Equal (RemoteObjectType.Object, exceptionData.Type);
			Assert.Equal (RemoteObjectSubType.Error, exceptionData.SubType);
			Assert.Equal (TestConstants.MyErrorClassName, exceptionData.ClassName);
			// The message needs to end with a newline or it won't be displayed.
			Assert.StartsWith (TestConstants.MyErrorMessage + "\n", exceptionData.Description);
			Assert.StartsWith ("dotnet:exception:", exceptionData.ObjectId);
			Assert.False (exceptionData.Uncaught);

			Debug.WriteLine ("DONE");
		}

		protected async Task SetPauseOnExceptions (PauseOnExceptionMode mode)
		{
			var request = new SetPauseOnExceptionsRequest { State = mode };
			await SendCommand (request).ConfigureAwait (false);
		}
	}
}
