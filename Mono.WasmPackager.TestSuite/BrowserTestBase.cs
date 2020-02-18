using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Reflection;
using System.Diagnostics;

using WebAssembly.Net.Debugging;
using Mono.WasmPackager.DevServer;
using Newtonsoft.Json.Linq;
using PuppeteerSharp;
using Xunit;

namespace Mono.WasmPackager.TestSuite
{
	public abstract class BrowserTestBase : DebuggerTestBase
	{
		protected BrowserTestBase (Assembly caller = null)
			: base (caller ?? Assembly.GetCallingAssembly ())
		{
		}

		// InspectorClient client;
		Dictionary<string, TaskCompletionSource<JObject>> notifications = new Dictionary<string, TaskCompletionSource<JObject>> ();
		Dictionary<string, Func<JObject, CancellationToken, Task>> eventListeners = new Dictionary<string, Func<JObject, CancellationToken, Task>> ();

		public const string PAUSE = "pause";
		public const string READY = "ready";

		Browser browser;
		BrowserContext context;
		Page page;

		InspectorClient client;
		CancellationTokenSource cts;
		TaskCompletionSource<object> readyTcs;
		TaskCompletionSource<object> completedTcs;

		protected InspectorClient Client => client;

		string ME => $"[{GetType ().Name}]";

		public override async Task InitializeAsync ()
		{
			await base.InitializeAsync ();

			cts = new CancellationTokenSource ();
			completedTcs = new TaskCompletionSource<object> ();
			readyTcs = new TaskCompletionSource<object> ();

			var chromiumPath = await TestSuiteSetup.GetChromiumPath ();

			var options = new LaunchOptions ()
			{
				Headless = true,
				Args = new[] {
					"--disable-gpu",
					$"--remote-debugging-port=0"
				},
				IgnoreDefaultArgs = false,
				ExecutablePath = chromiumPath
			};

			browser = await PuppeteerSharp.Puppeteer.LaunchAsync (options);

			context = await browser.CreateIncognitoBrowserContextAsync ();
			page = await context.NewPageAsync ();

			var url = $"http://localhost:{Server.ServerOptions.FileServerPort}/{Server.ServerOptions.PagePath}";

			await page.GoToAsync (url);

			Debug.WriteLine ($"{ME} loaded page {url} - {page.Target.TargetId}");

			client = new InspectorClient ();

			readyTcs = new TaskCompletionSource<object> ();
			LaunchProxy (page.Target);
			await readyTcs.Task;
		}

		public override async Task DisposeAsync ()
		{
			completedTcs.TrySetResult (null);

			if (client != null) {
				await client.Close (cts.Token);
				client.Dispose ();
				client = null;
			}

			cts.Cancel ();
			cts.Dispose ();

			if (browser != null) {
				browser.Dispose ();
				browser = null;
			}
			if (page != null) {
				page.Dispose ();
				page = null;
			}
			await base.DisposeAsync ();
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
			// Debug.WriteLine ("OnMessage " + method + args);
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

		async void LaunchProxy (Target target)
		{
			var browserUri = new Uri (browser.WebSocketEndpoint);
			var uri = new Uri ($"ws://localhost:{Server.ServerOptions.DebugServerPort}/connect-to-puppeteer?puppeteer-port={browserUri.Port}&page-id={target.TargetId}");

			await client.Connect (uri, OnMessage, async token =>
			{
				Task[] init_cmds = {
					client.SendCommand ("Profiler.enable", null, token),
					client.SendCommand ("Runtime.enable", null, token),
					client.SendCommand ("Debugger.enable", null, token),
					client.SendCommand ("Runtime.runIfWaitingForDebugger", null, token),
					WaitFor (READY)
				};
				// await Task.WhenAll (init_cmds);
				Debug.WriteLine ("waiting for the runtime to be ready");
				await init_cmds[4];
				Debug.WriteLine ("runtime ready, TEST TIME");

				readyTcs.TrySetResult (null);

				await completedTcs.Task;

				Debug.WriteLine ("inner completed");
			}, cts.Token);

			Debug.WriteLine ($"CONNECT DONE!");

			try {
				await client.Close (cts.Token);
			} catch {
				; // ignore
			}
		}

		public Task<Result> SendCommand (string message, JObject args)
		{
			return client.SendCommand (message, args, cts.Token);
		}
	}
}
