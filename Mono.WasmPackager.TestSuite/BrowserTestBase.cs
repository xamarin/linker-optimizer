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
		CDPSession session;

		NewDevToolsClient client;
		AbstractConnection connection;
		CancellationTokenSource cts;

		protected Page Page => page;

		protected virtual bool Headless => true;

		string ME => $"[{GetType ().Name}]";

		public override async Task InitializeAsync ()
		{
			await base.InitializeAsync ();

			cts = new CancellationTokenSource ();

			var chromiumPath = await TestSuiteSetup.GetChromiumPath ();

			var options = new LaunchOptions () {
				Headless = Headless,
				Args = new [] {
					"--disable-gpu",
					$"--remote-debugging-port=0"
				},
				IgnoreDefaultArgs = false,
				ExecutablePath = chromiumPath
			};

			browser = await PuppeteerSharp.Puppeteer.LaunchAsync (options);

			context = await browser.CreateIncognitoBrowserContextAsync ();
			page = await context.NewPageAsync ();

			session = await page.Target.CreateCDPSessionAsync ();

			var url = $"http://localhost:{Server.ServerOptions.FileServerPort}/{Server.ServerOptions.PagePath}";

			await page.GoToAsync (url);

			Debug.WriteLine ($"{ME} loaded page {url} - {page.Target.TargetId}");

			var targetId = page.Target.TargetId;
			var uri = new Uri ($"ws://localhost:{Server.ServerOptions.DebugServerPort}/connect-to-puppeteer?instance-id={targetId}");

			if (!TestHarnessStartup.Registration.TryAdd (targetId, session))
				throw new InvalidOperationException ($"Failed to register target '{targetId}'.");

			connection = new ClientWebSocketConnection (uri, null);

			try {
				client = new NewDevToolsClient (connection);

				await connection.Start (OnEvent, cts.Token).ConfigureAwait (false);

				Task [] init_cmds = {
					client.SendCommand ("Profiler.enable", null, cts.Token),
					client.SendCommand ("Runtime.enable", null, cts.Token),
					client.SendCommand ("Debugger.enable", null, cts.Token),
					client.SendCommand ("Runtime.runIfWaitingForDebugger", null, cts.Token),
					WaitFor (READY)
				};
				// await Task.WhenAll (init_cmds);
				Debug.WriteLine ("waiting for the runtime to be ready");

				while (true) {
					var delay = Task.Delay (2500);
					var task = await Task.WhenAny (delay, init_cmds [4]);
					if (task != delay)
						break;
					Debug.WriteLine ($"STILL WAITING");
				}

				Debug.WriteLine ($"GOT READY EVENT");
			} finally {
				TestHarnessStartup.Registration.Remove (targetId, out var _);
			}
		}

		public override async Task DisposeAsync ()
		{
			if (client != null) {
				await client.Close (cts.Token);
				client.Dispose ();
				client = null;
			}

			if (connection != null) {
				connection.Dispose ();
				connection = null;
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
			notifications [what] = n;
			return n.Task;
		}

		void NotifyOf (string what, JObject args)
		{
			if (!notifications.ContainsKey (what))
				throw new Exception ($"Invalid internal state, notifying of {what}, but nobody waiting");
			notifications [what].SetResult (args);
			notifications.Remove (what);
		}

		public void On (string evtName, Func<JObject, CancellationToken, Task> cb)
		{
			eventListeners [evtName] = cb;
		}

		async Task OnEvent (ConnectionEventArgs args, CancellationToken token)
		{
			switch (args.Message) {
			case "Debugger.paused":
				NotifyOf (PAUSE, args.Arguments);
				break;
			case "Mono.runtimeReady":
				NotifyOf (READY, args.Arguments);
				break;
			case "Runtime.DebugAPICalled":
				Debug.WriteLine ("CWL: {0}", args.Arguments? ["args"]? [0]? ["value"]);
				break;
			case "Debugger.scriptParsed":
			case "Runtime.consoleAPICalled":
				break;
			default:
				Debug.WriteLine ($"ON MESSAGE: {args.Message} {args.Arguments}");
				break;
			}
			if (eventListeners.ContainsKey (args.Message))
				await eventListeners [args.Message] (args.Arguments, token);
		}

		public Task<Result> SendCommand (string message, JObject args)
		{
			return client.SendCommand (message, args, cts.Token);
		}
	}
}
