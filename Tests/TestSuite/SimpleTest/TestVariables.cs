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

namespace SimpleTest
{
	public class TestVariables : PuppeteerTestBase
	{
		[Fact]
		public async Task Test ()
		{
			Assert.Equal (TestConstants.TextReady, await GetInnerHtml ("#output"));

			var id = await InsertBreakpoint (TestSettings.Locations.VariableTest);

			var pause = WaitForPaused ();
			var click = ClickAndWaitForMessage ("#variables", TestConstants.VariablesText);

			var notification = await pause.ConfigureAwait (false);

			AssertBreakpointHit (id, notification);

			var frame = notification.CallFrames [0];

			await GetProperties (frame);

			var piOverE = await EvaluateOnCallFrame (frame, "piOverE");
			var message = await EvaluateOnCallFrame (frame, "message");
			AssertString (message);

			var obj = await EvaluateOnCallFrame (frame, "obj");

			await EvaluateOnCallFrame (frame, "obj.PropertyThrows");

			Debug.WriteLine ($"DONE");
		}

		void AssertString (RemoteObject remoteObject)
		{
			Debug.WriteLine ($"OBJECT: {remoteObject}");
		}

		async Task GetProperties (CallFrame frame)
		{
			var request = new GetPropertiesRequest { ObjectId = frame.CallFrameId };
			var response = await SendCommand<GetPropertiesResponse> ("Runtime.getProperties", request);
			Debug.WriteLine ($"RESPONSE: {response}");
		}

		async Task<RemoteObject> EvaluateOnCallFrame (CallFrame frame, string expression)
		{
			var request = new EvaluateOnCallFrameRequest { CallFrameId = frame.CallFrameId, Expression = expression };
			var response = await SendCommand<EvaluateOnCallFrameResponse> ("Debugger.evaluateOnCallFrame", request);
			Debug.WriteLine ($"RESPONSE: {response}");
			return response.Result;
		}
	}
}
