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

namespace Mono.WasmPackager.DevServer
{
	public abstract class NewDevToolsProxy : IDisposable
	{
		TaskCompletionSource<bool> side_exception = new TaskCompletionSource<bool> ();
		TaskCompletionSource<bool> client_initiated_close = new TaskCompletionSource<bool> ();
		AbstractConnection ideConnection;
		AbstractConnection browserConnection;
		AsyncQueue<ConnectionEventArgs> eventQueue;
		TaskCompletionSource<bool> exitTcs;
		int disposed;

		protected NewDevToolsProxy (AbstractConnection browserConnection, AbstractConnection ideConnection)
		{
			this.browserConnection = browserConnection;
			this.ideConnection = ideConnection;
			exitTcs = new TaskCompletionSource<bool> ();
		}

		protected abstract void AcceptEvent (SessionId sessionId, ConnectionEventArgs eventArgs);

		protected abstract void AcceptCommand (MessageId id, ConnectionEventArgs eventArgs);

		internal async Task<Result> SendCommand (SessionId id, string method, JObject args, CancellationToken token)
		{
			Log ("verbose", $"sending command {method}: {args}");
			if (id?.sessionId != null && id.sessionId != browserConnection.SessionId)
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

		public async Task SendEvent (SessionId sessionId, string method, JObject args, CancellationToken token)
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
				Log ("verbose", $"BROWSER MESSAGE: {args.Message} {args.Arguments}");
				var sessionId = new SessionId { sessionId = args.SessionId };
				AcceptEvent (sessionId, args);
				if (args.Handler != null)
					eventQueue.Enqueue (args);
				else if (!args.SkipEvent)
					await SendEvent (sessionId, args.Message, args.Arguments, token).ConfigureAwait (false);
			}, x.Token);

			await ideConnection.Start (async (args, token) => {
				Log ("verbose", $"IDE MESSAGE: {args.Message} {args.Arguments}");
				var id = new MessageId { id = args.Id, sessionId = args.SessionId };
				AcceptCommand (id, args);
				if (args.Handler != null)
					ideQueue.Enqueue (args);
				else if (!args.SkipEvent) {
					var res = await SendCommand (id, args.Message, args.Arguments, token);
					await SendResponse (id, res, token);
				}
			}, x.Token);
		}

		public Task WaitForExit () => exitTcs.Task;

		protected void Log (string priority, string msg)
		{
			switch (priority) {
			case "protocol":
				// Debug.WriteLine (msg);
				break;
			case "verbose":
				// Debug.WriteLine (msg);
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
		}
	}
}
