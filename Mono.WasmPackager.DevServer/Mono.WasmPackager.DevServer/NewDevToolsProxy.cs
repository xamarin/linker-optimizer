using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using WebAssembly.Net.Debugging;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using P = PuppeteerSharp;

namespace Mono.WasmPackager.DevServer
{
	public class NewDevToolsProxy : IDisposable
	{
		MonoProxy proxy;
		AbstractConnection ideConnection;
		AbstractConnection browserConnection;
		AsyncQueue<ConnectionEventArgs> eventQueue;
		TaskCompletionSource<bool> exitTcs;
		ILoggerFactory loggerFactory;
		ILogger logger;
		int disposed;

		NewDevToolsProxy (AbstractConnection browserConnection, AbstractConnection ideConnection)
		{
			this.browserConnection = browserConnection;
			this.ideConnection = ideConnection;

			loggerFactory = new LoggerFactory ();
			logger = loggerFactory.CreateLogger<NewDevToolsProxy> ();
			exitTcs = new TaskCompletionSource<bool> ();
			proxy = new MonoProxy (browserConnection, ideConnection, this, loggerFactory);
		}

		public static NewDevToolsProxy Create (TestSession session, WebSocketManager manager)
		{
			var browserConnection = new PuppeteerConnection (session, "browser");
			var ideConnection = new ServerWebSocketConnection (manager, session.Session.SessionId, "ide");
			return new NewDevToolsProxy (browserConnection, ideConnection);
		}

		public static NewDevToolsProxy Create (Uri endpoint, WebSocketManager manager, string sessionId = null)
		{
			var browserConnection = new ClientWebSocketConnection (endpoint, sessionId, "brower");
			var ideConnection = new ServerWebSocketConnection (manager, sessionId, "ide");
			return new NewDevToolsProxy (browserConnection, ideConnection);
		}

		internal async Task<Result> SendCommand (SessionId id, string method, JObject args, CancellationToken token)
		{
			Log ("verbose", $"sending command {method}: {args}");
			if (id.sessionId != null && id.sessionId != browserConnection.SessionId)
				throw new InvalidOperationException ();

			Log ("verbose", $"sending to browser: {method} {args}");

			try {
				var result = await browserConnection.SendAsync (id, method, args).ConfigureAwait (false);
				Log ("verbose", $"sending to browser - response: {method} {result}");
				return Result.FromJson (result);
			} catch (Exception e) {
				Log ("verbose", $"sending to browser - error: {method} {e.Message}");
				return Result.Exception (e);
			}
		}

		internal async Task SendEvent (SessionId sessionId, string method, JObject args, CancellationToken token)
		{
			Log ("verbose", $"sending to ide: {method} {args}");
			await ideConnection.SendAsync (sessionId, method, args, false).ConfigureAwait (false);
		}

		internal async Task SendResponse (MessageId id, Result result, CancellationToken token)
		{
			Log ("verbose", $"sending response to ide: {id}: {result.ToJObject (id)}");
			await ideConnection.SendAsync (id, null, result.ToJObject (id), false).ConfigureAwait (false);
		}

		async Task AsyncEventHandler (string sender, ConnectionEventArgs args, CancellationToken token)
		{
			Log ("protocol", $"Queued {sender} event: {args}");
			await args.Handler (token).ConfigureAwait (false);
			Log ("protocol", $"queued {sender} event done: {args}");
		}

		public async Task Start ()
		{
			Log ("info", $"DevToolsProxy starting");

			var x = new CancellationTokenSource ();

			eventQueue = new AsyncQueue<ConnectionEventArgs> (false, "proxy-events");
			eventQueue.Start ((args, token) => AsyncEventHandler ("browser", args, token));

			var ideQueue = new AsyncQueue<ConnectionEventArgs> (true, "proxy-commands");
			ideQueue.Start ((args, token) => AsyncEventHandler ("ide", args, token));

			await browserConnection.Start (async (args, token) => {
				//
				// We are called from within the browser connection's main read loop,
				// so no new events or replies are read from the browser until we return.
				//
				// This means that we need to be very careful about which kinds of async
				// operations we may perform in here.
				//
				// Whenever we receive an event that we're going to pass through to the IDE,
				// then we do so right away.
				//
				// Otherwise, we queue the event and process them one at a time.
				//
				if (args.Close) {
					await ideConnection.Close (false, token).ConfigureAwait (false);
					exitTcs.TrySetResult (false);
					return;
				}
				Log ("verbose", $"BROWSER MESSAGE: {args.Message} {args.Arguments}");
				var sessionId = new SessionId (args.SessionId);
				proxy.AcceptEvent (sessionId, args);
				if (args.Handler != null)
					eventQueue.Enqueue (args);
				else if (!args.SkipEvent)
					await SendEvent (sessionId, args.Message, args.Arguments, token).ConfigureAwait (false);
			}, x.Token);

			await ideConnection.Start (async (args, token) => {
				if (args.Close) {
					await browserConnection.Close (false, token).ConfigureAwait (false);
					exitTcs.TrySetResult (false);
					return;
				}
				Log ("verbose", $"IDE MESSAGE: {args.Message} {args.Arguments}");
				var id = new MessageId (args.SessionId, args.Id);
				proxy.AcceptCommand (id, args);
				if (args.Handler != null)
					ideQueue.Enqueue (args);
				else if (!args.SkipEvent) {
					var res = await SendCommand (id, args.Message, args.Arguments, token);
					await SendResponse (id, res, token);
				}
			}, x.Token);
		}

		public async Task WaitForExit ()
		{
			await exitTcs.Task.ConfigureAwait (false);
			await ideConnection.Close (false, CancellationToken.None).ConfigureAwait (false);
			await browserConnection.Close (false, CancellationToken.None).ConfigureAwait (false);
		}

		protected internal void Log (string priority, string msg)
		{
			switch (priority) {
			case "protocol":
				Debug.WriteLine (msg);
				break;
			case "verbose":
				Debug.WriteLine (msg);
				break;
			case "info":
			case "warning":
			case "error":
			default:
				Debug.WriteLine (msg);
				break;
			}
		}

		public void Dispose ()
		{
			if (Interlocked.CompareExchange (ref disposed, 1, 0) == 0)
				DoDispose ();
		}

		protected virtual void DoDispose ()
		{
			browserConnection.Dispose ();
			ideConnection.Dispose ();
			loggerFactory.Dispose ();
		}
	}
}
