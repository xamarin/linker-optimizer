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

namespace BlazorTests
{
	public class MartinTest : PuppeteerTestBase
	{
		protected override bool Headless => false;

		[Fact]
		public void Start ()
		{
			Debug.WriteLine ("MARTIN TEST!");
		}
	}
}
