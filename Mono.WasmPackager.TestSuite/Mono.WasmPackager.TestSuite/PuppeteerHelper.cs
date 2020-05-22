using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using PuppeteerSharp;
using Xunit;

namespace Mono.WasmPackager.TestSuite
{
	using Messaging.Debugger;

	public static class PuppeteerHelper
	{
		public static async Task<string> GetInnerHtml (this ElementHandle handle)
		{
			var property = await handle.GetPropertyAsync ("innerHTML");
			var value = property.RemoteObject.Value;
			return value.Value<string> ();
		}
	}
}
