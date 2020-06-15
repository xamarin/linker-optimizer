using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Reflection;
using System.Diagnostics;

using Mono.WasmPackager.DevServer;
using Newtonsoft.Json.Linq;
using PuppeteerSharp;

namespace Mono.WasmPackager.TestSuite
{
	using Messaging;
	using Messaging.Debugger;

	public abstract class BrowserTestBase : DebuggerTestBase
	{
		protected BrowserTestBase (Assembly caller = null)
			: base (caller ?? Assembly.GetCallingAssembly ())
		{
		}

		readonly Dictionary<string, TaskCompletionSource<JObject>> notifications = new Dictionary<string, TaskCompletionSource<JObject>> ();
		readonly Dictionary<string, Func<JObject, CancellationToken, Task>> eventListeners = new Dictionary<string, Func<JObject, CancellationToken, Task>> ();

		const string RESUME = "resume";
		const string PAUSE = "pause";
		const string READY = "ready";

		Browser browser;
		BrowserContext context;
		Page page;

		public TestSession Session {
			get; private set;
		}

		NewDevToolsClient client;
		AbstractConnection connection;
		CancellationTokenSource cts;
		TaskCompletionSource<Exception> errorTcs;

		internal Page InternalPage => page;

		volatile TaskCompletionSource<PausedNotification> currentCommand;

		protected virtual bool Headless => true;

		string ME => $"[{GetType ().Name}]";

		public override async Task InitializeAsync ()
		{
			await base.InitializeAsync ();

			errorTcs = new TaskCompletionSource<Exception> ();

			cts = new CancellationTokenSource ();
			cts.Token.Register (() => errorTcs.TrySetCanceled ());

			var chromiumPath = await TestSuiteSetup.GetChromiumPath (Settings);

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

			var session = await page.Target.CreateCDPSessionAsync ();

			Session = new TestSession (page.Target, session);

			var url = $"http://localhost:{Server.ServerOptions.FileServerPort}/{Server.ServerOptions.PagePath}";

			await page.GoToAsync (url);

			Debug.WriteLine ($"{ME} loaded page {url} - {page.Target.TargetId}");

			var targetId = page.Target.TargetId;
			var uri = new Uri ($"ws://localhost:{Server.ServerOptions.DebugProxyPort}/connect-to-puppeteer?instance-id={targetId}");

			if (!TestHarnessStartup.Registration.TryAdd (targetId, Session))
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
					var delay = Task.Delay (TimeSpan.FromSeconds (15));
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
			if (connection != null) {
				await connection.Close (false, cts.Token);
				connection.Dispose ();
				connection = null;
			}

			if (client != null) {
				await client.Close (true, cts.Token);
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
				throw OnError ($"Invalid internal state, waiting for {what} while another wait is already setup");
			var n = new TaskCompletionSource<JObject> ();
			notifications [what] = n;
			return CheckedWait (n.Task);
		}

		void NotifyOf (string what, JObject args)
		{
			if (!notifications.Remove (what, out var notification)) {
				Debug.WriteLine ($"Invalid internal state, notifying of {what}, but nobody waiting");
				if (string.Equals (what, PAUSE) && NotifyOfPause (args.ToObject<PausedNotification> ()))
					return;
				throw OnError ($"Invalid internal state, notifying of {what}, but nobody waiting");
			}

			notification.SetResult (args);
		}

		Exception OnError (string message)
		{
			var error = new Exception (message);
			errorTcs.TrySetException (error);
			throw error;
		}

		bool NotifyOfPause (PausedNotification notification)
		{
			var tcs = currentCommand;
			if (tcs == null)
				return false;

			tcs.TrySetResult (notification);
			return true;
		}

		public void On<T> (string evtName, Func<T, CancellationToken, Task> cb)
		{
			eventListeners [evtName] = (args, token) => {
				var obj = args.ToObject<T> ();
				return cb (obj, token);
			};
		}

		async Task OnEvent (ConnectionEventArgs args, CancellationToken token)
		{
			switch (args.Message) {
			case "Debugger.resumed":
				NotifyOf (RESUME, args.Arguments);
				break;
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

		public async Task<T> CheckedWait<T> (Task<T> task)
		{
			var result = await Task.WhenAny (task, errorTcs.Task);
			if (result == errorTcs.Task)
				throw errorTcs.Task.Result;
			return task.Result;
		}

		public async Task CheckedWait (Task task)
		{
			var result = await Task.WhenAny (task, errorTcs.Task);
			if (result == errorTcs.Task)
				throw errorTcs.Task.Result;
		}

		public Task<T[]> CheckedWaitAll<T> (params Task<T>[] tasks) => CheckedWait (Task.WhenAll (tasks));

		public Task CheckedWaitAll (params Task[] tasks) => CheckedWait (Task.WhenAll (tasks));

		void CheckError ()
		{
			if (errorTcs.Task.Status == TaskStatus.Faulted || errorTcs.Task.Status == TaskStatus.Canceled)
				throw errorTcs.Task.Result;
		}

		public Task<ElementHandle> QuerySelectorAsync (string selector) => CheckedWait (InternalPage.QuerySelectorAsync (selector));

		public async Task<T> SendCommand<T> (ProtocolRequest<T> request, string message = null)
			where T : ProtocolResponse
		{
			var jobj = JObject.FromObject (request);

			var tcs = new TaskCompletionSource<PausedNotification> ();
			if (Interlocked.CompareExchange (ref currentCommand, tcs, null) != null)
				throw new InvalidOperationException ($"{nameof (SendCommand)} is already running!");

			JObject response;
			try {
				var sendTask = client.SendCommand (message ?? request.Command, jobj, cts.Token);
				await Task.WhenAny (sendTask, tcs.Task, errorTcs.Task).ConfigureAwait (false);
				CheckError ();

				if (tcs.Task.Status == TaskStatus.RanToCompletion) {
					Debug.WriteLine ($"GOT PAUSED NOTIFICATION: {tcs.Task.Result}");
					throw new CommandErrorException ("Got pause notification during command", null);
				}

				response = sendTask.Result;
			} finally {
				currentCommand = null;
			}

			var result = response ["result"] as JObject;
			var error = response ["error"] as JObject;

			if (result != null && error != null)
				throw new ArgumentException ($"Both {nameof (result)} and {nameof (error)} arguments cannot be non-null.");

			if (error != null)
				throw new CommandErrorException (message, error);

			return result.ToObject<T> ();
		}

		public async Task<T> SendPageCommand<T> (ProtocolRequest<T> request, string message = null)
			where T : ProtocolResponse
		{
			var jobj = JObject.FromObject (request);
			var response = await CheckedWait (page.Client.SendAsync (message ?? request.Command, jobj)).ConfigureAwait (false);
			return response.ToObject<T> ();
		}

		protected async Task<PausedNotification> WaitForPaused ()
		{
			var paused = await WaitFor (PAUSE).ConfigureAwait (false);
			return paused.ToObject<PausedNotification> ();
		}

		protected Task WaitForResumed () => WaitFor (RESUME);
	}
}
