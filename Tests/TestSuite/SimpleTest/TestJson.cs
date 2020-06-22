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
	public class Json
	{
		const string ExtraField = "extraField";
		const string ExtraValue = "extraValue";

		[Fact]
		public void TestEncode ()
		{
			var request = new SetPauseOnExceptionsRequest { State = PauseOnExceptionMode.All };
			var jobj = JObject.FromObject (request);
			Assert.Equal ("{\n  \"state\": \"all\"\n}", jobj.ToString ());

			// Add an extra field that's not in the protocol.
			jobj[ExtraField] = ExtraValue;
			var deserialized = jobj.ToObject<SetPauseOnExceptionsRequest> ();
			Assert.NotNull (deserialized);

			// Make sure that it gets preserved in OriginalJToken.	
			Assert.NotNull (deserialized.OriginalJToken);
			Assert.NotNull (deserialized.OriginalJToken[ExtraField]);
			Assert.Equal (ExtraValue, deserialized.OriginalJToken[ExtraField]);

			Assert.Equal (PauseOnExceptionMode.All, deserialized.State);

			// Changing the instance here won't do anything because the serializer will use
			// the OriginalJToken whenever it is ppresent.
			deserialized.State = PauseOnExceptionMode.Uncaught;
			var roundTrip = JObject.FromObject (deserialized);
			Assert.Equal ($"{{\n  \"state\": \"all\",\n  \"{ExtraField}\": \"{ExtraValue}\"\n}}", roundTrip.ToString ());

			// Clear it and try again.
			deserialized.OriginalJToken = null;
			var modified = JObject.FromObject (deserialized);
			Assert.Equal ("{\n  \"state\": \"uncaught\"\n}", modified.ToString ());
		}
	}
}
