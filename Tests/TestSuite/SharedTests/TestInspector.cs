using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using Xunit;

using Mono.WasmPackager.TestSuite;
using Mono.WasmPackager.TestSuite.Messaging.Debugger;
using Mono.WasmPackager.DevServer;

namespace SharedTests
{
	public abstract class TestInspector : InspectorTestBase
	{
		public static SourceLocation Location => TestSettings.Locations.Message;

		protected void SharedTestScripts ()
		{
			Debug.WriteLine ($"SERVER READY: {ScriptsIdToUrl}");
			Debug.WriteLine ($"SERVER READY");

			Assert.True (ScriptsIdToUrl.ContainsValue ($"dotnet://{Settings.DevServer_Assembly}/{Location.File}"));
		}

		protected async Task SharedCreateGoodBreakpoint ()
		{
			Debug.WriteLine ($"SERVER READY: {ScriptsIdToUrl}");

			var fileUrl = $"dotnet://{Settings.DevServer_Assembly}/{Location.File}";
			var request = new InsertBreakpointRequest {
				LineNumber = Location.Line,
				Url = FileToUrl [fileUrl]
			};
			if (Location.Column != null)
				request.ColumnNumber = Location.Column.Value;

			var result = await SendCommand (request).ConfigureAwait (false);
			Assert.EndsWith (Path.GetFileName (Location.File), result.BreakpointId);
			Assert.Single (result.Locations);

			var loc = result.Locations [0];

			Assert.NotNull (loc.ScriptId);
			Assert.Equal (fileUrl, ScriptsIdToUrl [loc.ScriptId]);
			Assert.Equal (Location.Line, loc.LineNumber);
			if (Location.Column != null)
				Assert.Equal (Location.Column.Value, loc.ColumnNumber);
		}

		protected async Task SharedGetBreakpoints ()
		{
			Debug.WriteLine ($"SERVER READY: {ScriptsIdToUrl}");

			var request = new GetPossibleBreakpointsRequest {
				Start = new Location {
					ScriptId = FileToId [$"dotnet://{Settings.DevServer_Assembly}/{Location.File}"],
					LineNumber = 0,
					ColumnNumber = 0
				}
			};

			var response = await SendCommand (request).ConfigureAwait (false);
			Assert.True (response.Locations.Length > 1);
		}
	}
}
