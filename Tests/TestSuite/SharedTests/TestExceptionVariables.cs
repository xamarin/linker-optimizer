using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using Xunit;

using Mono.WasmPackager.TestSuite;
using Mono.WasmPackager.TestSuite.Messaging;
using Mono.WasmPackager.TestSuite.Messaging.Debugger;
using Mono.WasmPackager.TestSuite.Messaging.Runtime;
using Mono.WasmPackager.DevServer;

namespace SharedTests
{
	public abstract class TestExceptionVariables : PuppeteerTestBase
	{
		protected override bool Headless => false;

		// The .NET Test Explorer has some issues with [Theory]'s, so use two [Fact]'s when overriding.
		protected Task SharedTestJsException (bool usePuppeteer) => SharedTestJsException (usePuppeteer, false, false);

		protected Task SharedTestJsCaughtException (bool usePuppeteer) => SharedTestJsException (usePuppeteer, true, false);

		protected Task SharedTestJsSilentException (bool usePuppeteer) => SharedTestJsException (usePuppeteer, true, true);

		async Task SharedTestJsException (bool usePuppeteer, bool pause, bool silent)
		{
			Assert.Equal (TestConstants.TextReady, await GetInnerHtml (TestConstants.Selectors.Output));

			var breakpoint = await InsertBreakpoint (TestSettings.Locations.JsVariables).ConfigureAwait (false);

			if (pause) {
				await SendBrowserOrProxy (usePuppeteer, new DebuggerEnableRequest ());
				await SendBrowserOrProxy (usePuppeteer, new SetPauseOnExceptionsRequest { State = PauseOnExceptionMode.All });
			}

			await ClickWithPausedNotification (
				TestConstants.Selectors.JsVariables,
				async notification => {
					AssertBreakpointHit (breakpoint, notification);
					await AssertJsVariable (breakpoint.Location, "myError", usePuppeteer, silent, notification);
				}).ConfigureAwait (false);

			Debug.WriteLine ($"DONE");
		}

		protected async Task SharedTestJsThrownException (bool usePuppeteer)
		{
			Assert.Equal (TestConstants.TextReady, await GetInnerHtml (TestConstants.Selectors.Output));

			await SendBrowserOrProxy (usePuppeteer, new DebuggerEnableRequest ());
			await SendBrowserOrProxy (usePuppeteer, new SetPauseOnExceptionsRequest { State = PauseOnExceptionMode.All });

			var location = TestSettings.Locations.JsException;

			await ClickWithPausedNotification (
				TestConstants.Selectors.JsException,
				async notification => {
					Assert.Empty (notification.HitBreakpoints);
					Assert.Equal (StoppedReason.Exception, notification.Reason);
					Assert.NotNull (notification.Data);
					var data = notification.Data.ToObject<PausedExceptionData> ();
					Assert.NotNull (data);
					AssertExceptionData (data, location);
					await AssertJsVariable (location, "myError", usePuppeteer, false, notification);
				}).ConfigureAwait (false);

			Debug.WriteLine ($"DONE");
		}

		async Task AssertJsVariable (SourceLocation location, string name, bool usePuppeteer, bool silent, PausedNotification notification)
		{
			var frame = notification.CallFrames [0];
			var scope = frame.ScopeChain.First (s => s.Type == ScopeType.Local);

			var properties = await SendCommand (new GetPropertiesRequest { ObjectId = scope.Object.ObjectId }).ConfigureAwait (false);
			var myErrorProp = properties.Result.First (p => p.Name == name);
			AssertProperty (name, myErrorProp, location);

			var successResponse = await SendBrowserOrProxy (usePuppeteer, new EvaluateOnCallFrameRequest {
				CallFrameId = frame.CallFrameId,
				Expression = name,
				Silent = silent
			});
			AssertSuccessResult (successResponse, location);

			var errorResponse = await SendBrowserOrProxy (usePuppeteer, new EvaluateOnCallFrameRequest {
				CallFrameId = frame.CallFrameId,
				Expression = $"throw {name}",
				Silent = silent
			});
			AssertErrorResult (errorResponse, location);

			Debug.WriteLine ($"DONE");
		}

		void AssertSuccessResult (EvaluateOnCallFrameResponse response, SourceLocation location)
		{
			Assert.NotNull (response);
			Assert.NotNull (response.Result);
			AssertExceptionObject (response.Result, location);
			Assert.Null (response.ExceptionDetails);
		}

		void AssertErrorResult (EvaluateOnCallFrameResponse response, SourceLocation location)
		{
			Assert.NotNull (response);
			Assert.NotNull (response.Result);
			AssertExceptionObject (response.Result, location);
			Assert.NotNull (response.ExceptionDetails);
			AssertExceptionObject (response.ExceptionDetails.Exception, location);
			Assert.Equal ("Uncaught", response.ExceptionDetails.Text);
			Assert.Null (response.ExceptionDetails.Url);
			Assert.Equal (0, response.ExceptionDetails.LineNumber);
			Assert.Equal (0, response.ExceptionDetails.ColumnNumber);
			Assert.NotNull (response.ExceptionDetails.ScriptId);
		}

		void AssertProperty (string name, PropertyDescriptor prop, SourceLocation location)
		{
			Debug.WriteLine ($"PROPERTY: {prop}");
			Assert.Equal (name, prop.Name);
			Assert.True (prop.Configurable);
			Assert.True (prop.Enumerable);
			Assert.True (prop.IsOwn);
			Assert.Null (prop.Get);
			Assert.Null (prop.Set);
			Assert.Null (prop.Symbol);
			Assert.False (prop.WasThrown);
			Assert.True (prop.Writable);
			Assert.NotNull (prop.Value);
			AssertExceptionObject (prop.Value, location);
		}

		void AssertExceptionObject (RemoteObject obj, SourceLocation location)
		{
			Assert.Equal ("MyError", obj.ClassName);
			Assert.Equal (RemoteObjectType.Object, obj.Type);
			Assert.Equal (RemoteObjectSubType.Error, obj.SubType);
			Assert.NotNull (obj.ObjectId);
			Assert.StartsWith ($"Error: MY ERROR\n    at {location.FunctionName} ", obj.Description);
		}

		void AssertExceptionData (PausedExceptionData data, SourceLocation location)
		{
			Assert.Equal (RemoteObjectType.Object, data.Type);
			Assert.Equal (RemoteObjectSubType.Error, data.SubType);
			Assert.Equal ("MyError", data.ClassName);
			// The message needs to end with a newline or it won't be displayed.
			Assert.StartsWith ($"Error: MY ERROR\n    at {location.FunctionName} ", data.Description);
			Assert.True (data.Uncaught);
		}

		Task<T> SendBrowserOrProxy<T> (bool usePuppeteer, ProtocolRequest<T> request, string message = null)
			where T : ProtocolResponse
		{
			if (usePuppeteer)
				return SendPageCommand (request, message);
			else
				return SendCommand (request, message);
		}
	}
}
