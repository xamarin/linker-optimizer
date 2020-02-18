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
	public class Inspector
	{
		// InspectorClient client;
		Dictionary<string, TaskCompletionSource<JObject>> notifications = new Dictionary<string, TaskCompletionSource<JObject>> ();
		Dictionary<string, Func<JObject, CancellationToken, Task>> eventListeners = new Dictionary<string, Func<JObject, CancellationToken, Task>> ();

		public const string PAUSE = "pause";
		public const string READY = "ready";

		public Server Server {
			get;
		}

		public Inspector (Server server)
		{
			Server = server;
		}

		public Task<JObject> WaitFor (string what)
		{
			if (notifications.ContainsKey (what))
				throw new Exception ($"Invalid internal state, waiting for {what} while another wait is already setup");
			var n = new TaskCompletionSource<JObject> ();
			notifications[what] = n;
			return n.Task;
		}

		void NotifyOf (string what, JObject args)
		{
			if (!notifications.ContainsKey (what))
				throw new Exception ($"Invalid internal state, notifying of {what}, but nobody waiting");
			notifications[what].SetResult (args);
			notifications.Remove (what);
		}

		public void On (string evtName, Func<JObject, CancellationToken, Task> cb)
		{
			eventListeners[evtName] = cb;
		}

		async Task OnMessage (string method, JObject args, CancellationToken token)
		{
			//System.Debug.WriteLine("OnMessage " + method + args);
			switch (method) {
				case "Debugger.paused":
					NotifyOf (PAUSE, args);
					break;
				case "Mono.runtimeReady":
					NotifyOf (READY, args);
					break;
				case "Runtime.DebugAPICalled":
					Debug.WriteLine ("CWL: {0}", args?["args"]?[0]?["value"]);
					break;
			}
			if (eventListeners.ContainsKey (method))
				await eventListeners[method] (args, token);
		}

		public async Task Ready (Func<InspectorClient, CancellationToken, Task> cb = null, TimeSpan? span = null)
		{
			using (var cts = new CancellationTokenSource ()) {
				cts.CancelAfter (span?.Milliseconds ?? 60 * 1000); //tests have 1 minute to complete by default
				var uri = new Uri ($"ws://{Server.ServerOptions.DebugServerUri.Authority}/launch-chrome-and-connect");
				using (var client = new InspectorClient ()) {
					await client.Connect (uri, OnMessage, async token =>
					{
						Task[] init_cmds = {
							client.SendCommand ("Profiler.enable", null, token),
							client.SendCommand ("Runtime.enable", null, token),
							client.SendCommand ("Debugger.enable", null, token),
							client.SendCommand ("Runtime.runIfWaitingForDebugger", null, token),
							WaitFor (READY),
						};
						// await Task.WhenAll (init_cmds);
						Debug.WriteLine ("waiting for the runtime to be ready");
						await init_cmds[4];
						Debug.WriteLine ("runtime ready, TEST TIME");
						if (cb != null) {
							Debug.WriteLine ("await cb(client, token)");
							await cb (client, token);
						}

					}, cts.Token);
					await client.Close (cts.Token);
				}
			}
		}
	}
}
