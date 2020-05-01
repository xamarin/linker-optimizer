using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using Xunit;

using Mono.WasmPackager.TestSuite;
using Mono.WasmPackager.TestSuite.Messaging.Debugger;
using Mono.WasmPackager.DevServer;

namespace WorkingTests
{
	public class TestInspector : InspectorTestBase
	{
		[Fact]
		public void TestScripts ()
		{
			Debug.WriteLine ($"SERVER READY: {ScriptsIdToUrl}");
			Debug.WriteLine ($"SERVER READY");

			Assert.True (ScriptsIdToUrl.ContainsValue ($"dotnet://{Settings.DevServer_Assembly}/Hello.cs"));
		}

		[Fact]
		public async Task CreateGoodBreakpoint ()
		{
			Debug.WriteLine ($"SERVER READY: {ScriptsIdToUrl}");

			var fileUrl = $"dotnet://{Settings.DevServer_Assembly}/Hello.cs";
			var request = new InsertBreakpointRequest {
				LineNumber = 8,
				ColumnNumber = 3,
				Url = FileToUrl [fileUrl]
			};

			var result = await SendCommand<InsertBreakpointResponse> ("Debugger.setBreakpointByUrl", request);
			Assert.EndsWith ("Hello.cs", result.BreakpointId);
			Assert.Single (result.Locations);

			var loc = result.Locations [0];

			Assert.NotNull (loc.ScriptId);
			Assert.Equal (fileUrl, ScriptsIdToUrl [loc.ScriptId]);
			Assert.Equal (8, loc.LineNumber);
			Assert.Equal (3, loc.ColumnNumber);
		}

		[Fact]
		public async Task GetBreakpoints ()
		{
			Debug.WriteLine ($"SERVER READY: {ScriptsIdToUrl}");

			var request = new GetPossibleBreakpointsRequest {
				Start = new Location {
					ScriptId = FileToId [$"dotnet://{Settings.DevServer_Assembly}/Hello.cs"],
					LineNumber = 0,
					ColumnNumber = 0
				}
			};

			var response = await SendCommand<GetPossibleBreakpointsResponse> ("Debugger.getPossibleBreakpoints", request);
			Assert.True (response.Locations.Length > 1);
		}
	}
}
