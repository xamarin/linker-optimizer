using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using Xunit;

using Mono.WasmPackager.TestSuite;
using Mono.WasmPackager.DevServer;

namespace SimpleTest
{
	public class TestPuppeteer : DebuggerTestBase
	{
		[Fact]
		public async Task TestBrowser ()
		{
			await BrowserController.Connect ("http://www.microsoft.com/");
			Debug.WriteLine ("DONE");
		}
	}
}
