using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

using System.Net.WebSockets;
using System.Collections.Generic;

using WebAssembly.Net.Debugging;
using Mono.WasmPackager.DevServer;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Mono.WasmPackager.TestSuite
{
	using Messaging.Debugger;

	public abstract class InspectorTestBase : BrowserTestBase
	{
		protected Dictionary<string, string> ScriptsIdToUrl {
			get;
		}

		protected Dictionary<string, string> FileToUrl {
			get;
		}

		protected Dictionary<string, string> FileToId {
			get;
		}

		protected InspectorTestBase (Assembly caller = null)
			: base (caller ?? Assembly.GetCallingAssembly ())
		{
			ScriptsIdToUrl = new Dictionary<string, string> ();
			FileToUrl = new Dictionary<string, string> ();
			FileToId = new Dictionary<string, string> ();
			SubscribeToScripts ();
		}

		void SubscribeToScripts ()
		{
			On<ScriptParsedNotification> ("Debugger.scriptParsed", async (args, c) => {
				if (!args.ScriptId.StartsWith ("dotnet://"))
					return;
				ScriptsIdToUrl [args.ScriptId] = args.DotNetUrl;
				FileToUrl [args.DotNetUrl] = args.Url;
				FileToId [args.DotNetUrl] = args.ScriptId;
				await Task.FromResult (0);
			});
		}
	}
}