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
	public class PuppeteerInstance : BrowserTestBase
	{
		[Fact]
		public async Task Start ()
		{
			Debug.WriteLine ("START");

			await Task.Delay (TimeSpan.FromHours (1));

			Debug.WriteLine ("DONE");
		}
	}
}
