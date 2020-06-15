using System;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using WebAssembly.Net.Debugging;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Mono.WasmPackager.DevServer {
	public class NewDevToolsProxy : IDisposable {
		readonly MonoProxy proxy;
		readonly AbstractConnection ideConnection;
		readonly AbstractConnection browserConnection;
		readonly AsyncQueue<ConnectionEventArgs> eventQueue;
		readonly AsyncQueue<ConnectionEventArgs> ideQueue;
		readonly TaskCompletionSource<bool> exitTcs;
		readonly ILoggerFactory loggerFactory;
		int disposed;

		NewDevToolsProxy (AbstractConnection browserConnection, AbstractConnection ideConnection)
		{
			this.browserConnection = browserConnection;
			this.ideConnection = ideConnection;

			loggerFactory = new LoggerFactory ();
			exitTcs = new TaskCompletionSource<bool> ();
			proxy = new MonoProxy (this, loggerFactory);
			eventQueue = new AsyncQueue<ConnectionEventArgs> (false, "proxy-events");
			ideQueue = new AsyncQueue<ConnectionEventArgs> (true, "proxy-commands");
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

		internal async Task<Result> SendCommand (SessionId id, string method, JObject args, CancellationToken _)
		{
			LogProtocol (method, "sending command", args);
			if (id.sessionId != null && id.sessionId != browserConnection.SessionId)
				throw new InvalidOperationException ();

			try {
				var result = await browserConnection.SendAsync (id, method, args).ConfigureAwait (false);
				LogProtocol (method, "sending to browser - response", result);
				return Result.FromJson (result);
			} catch (Exception e) {
				LogProtocol (method, "sending to browser - error", e.Message);
				return Result.Exception (e);
			}
		}

		async Task ProxyCommand (MessageId id, ConnectionEventArgs args)
		{
			LogProtocol (args.Message, "proxy command", args.Arguments);
			if (id.sessionId != null && id.sessionId != browserConnection.SessionId)
				throw new InvalidOperationException ();

			JObject result;
			try {
				result = await browserConnection.SendAsync (id, args.Message, args.Arguments).ConfigureAwait (false);
				LogProtocol (args.Message, "sending to browser - proxy response", result);
			} catch (Exception e) {
				LogProtocol (args.Message, "sending to browser - error", e.Message);
				result = Result.Exception (e).ToJObject (id);
			}

			result["id"] = id.id;
			result["sessionId"] = id.sessionId;

			await ideConnection.SendAsync (id, null, result, false).ConfigureAwait (false);
		}

		internal async Task SendEvent (SessionId sessionId, string method, JObject args, CancellationToken _)
		{
			LogProtocol (method, "sending to ide", args);
			await ideConnection.SendAsync (sessionId, method, args, false).ConfigureAwait (false);
		}

		internal async Task SendResponse (MessageId id, Result result, CancellationToken _)
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

			eventQueue.Start ((args, token) => AsyncEventHandler ("browser", args, token));

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
				LogProtocol (args.Message, "BROWSER MESSAGE", args.Arguments);
				var sessionId = new SessionId (args.SessionId);
				var command = proxy.AcceptEvent (sessionId, args);

				async ValueTask Continuation (CommandResult result)
				{
					if (result.ProxyCommand)
						await SendEvent (sessionId, args.Message, args.Arguments, token).ConfigureAwait (false);
					else if (result.HasResult)
						throw new InvalidOperationException ();
				}

				if (command.Handler != null) {
					args.Handler = async token => {
						var result = await command.Handler (token).ConfigureAwait (false);
						await Continuation (result);
					};
					eventQueue.Enqueue (args);
				} else {
					await Continuation (command.Result);
				}
			}, x.Token);

			await ideConnection.Start (async (args, token) => {
				if (args.Close) {
					await browserConnection.Close (false, token).ConfigureAwait (false);
					exitTcs.TrySetResult (false);
					return;
				}
				LogProtocol (args.Message, "IDE MESSAGE", args.Arguments);
				var id = new MessageId (args.SessionId, args.Id);
				var command = proxy.AcceptCommand (id, args);

				async ValueTask Continuation (CommandResult result)
				{
					if (result.ProxyCommand) {
						await ProxyCommand (id, args).ConfigureAwait (false);
					} else if (result.HasResult) {
						await SendResponse (id, result.Result, token).ConfigureAwait (false);
					}
				}

				if (command.Handler != null) {
					args.Handler = async token => {
						var result = await command.Handler (token).ConfigureAwait (false);
						await Continuation (result);
					};
					ideQueue.Enqueue (args);
				} else {
					await Continuation (command.Result).ConfigureAwait (false);
				}
			}, x.Token);
		}

		public async Task WaitForExit ()
		{
			await exitTcs.Task.ConfigureAwait (false);
			await ideConnection.Close (false, CancellationToken.None).ConfigureAwait (false);
			await browserConnection.Close (false, CancellationToken.None).ConfigureAwait (false);
		}

		protected internal void LogProtocol (string method, string msg, object args)
		{
			LoggingHelper.LogProtocol (this, method, msg, args);
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
