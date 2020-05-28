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
	public class Hello : DebuggerTestBase
	{
		[Fact]
		public void StartServer ()
		{
			Debug.WriteLine ($"SERVER READY");
		}
	}
}
