using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using WebAssembly.Net.Debugging;

namespace Mono.WasmPackager.DevServer
{
	public abstract class WebSocketConnection : AbstractConnection
	{
		WebSocket socket;
		readonly AsyncQueue<Command> sendQueue;
		protected List<(int, Command)> pendingCmds = new List<(int, Command)> ();
		protected int next_cmd_id;

		protected WebSocketConnection (string sessionId, string name)
			: base (sessionId, name)
		{
			sendQueue = new AsyncQueue<Command> (false, $"{Name}:send-queue");
		}

		protected sealed override async Task Start (CancellationToken token)
		{
			socket = await CreateSocket (token).ConfigureAwait (false);

			sendQueue.Start (Send);

			MainLoop (token);
		}

		protected abstract Task<WebSocket> CreateSocket (CancellationToken token);

		async Task<bool> ReadOne (CancellationToken token)
		{
			byte [] buff = new byte [4000];
			var mem = new MemoryStream ();
			var complete = false;

			while (!complete && !token.IsCancellationRequested) {
				if (socket.State == WebSocketState.CloseReceived || socket.State == WebSocketState.Aborted)
					return false;
				var result = await socket.ReceiveAsync (new ArraySegment<byte> (buff), CancellationToken.None);
				if (result.MessageType == WebSocketMessageType.Close) {
					await OnEvent (new ConnectionEventArgs { Close = true }).ConfigureAwait (false);
					return false;
				}

				mem.Write (buff, 0, result.Count);

				if (!result.EndOfMessage)
					continue;

				complete = true;

				var message = JObject.Parse (Encoding.UTF8.GetString (mem.GetBuffer (), 0, (int)mem.Length));

				var args = Decode (message);
				if (args != null)
					await OnEvent (args);
			}

			return !token.IsCancellationRequested;
		}

		protected abstract ConnectionEventArgs Decode (JObject message);

		protected class Command
		{
			public string SessionId {
				get;
			}

			public string Method {
				get;
			}

			public object Arguments {
				get;
			}

			public TaskCompletionSource<JObject> Completion {
				get;
			}

			public JObject Encoded {
				get; set;
			}

			public Command (string sessionId, string method, object args, bool wait)
			{
				SessionId = sessionId;
				Method = method;
				Arguments = args;
				if (wait)
					Completion = new TaskCompletionSource<JObject> ();
			}
		}

		internal override async Task<JObject> SendAsync (SessionId sessionId, string method, object args = null, bool waitForCallback = true)
		{
			LogProtocol (method, "SEND COMMAND");
			var command = new Command (sessionId.sessionId ?? SessionId, method, args, waitForCallback);
			command.Encoded = Encode (command);
			await sendQueue.EnqueueAsync (command).ConfigureAwait (false);
			if (!waitForCallback)
				return null;
			var result = await command.Completion.Task.ConfigureAwait (false);
			LogProtocol (method, "SEND COMMAND DONE", result);
			return result;
		}

		[Conditional ("CONNECTION_DEBUG")]
		void Log (string msg)
		{
			Debug.WriteLine ($"[{GetType ().Name}]: {msg}");
		}

		[Conditional ("CONNECTION_DEBUG")]
		protected internal void LogProtocol (string method, string msg, object args = null)
		{
			LoggingHelper.LogProtocol (this, method, msg, args);
		}

		public override async Task Close (bool wait, CancellationToken cancellationToken)
		{
			Log ($"CLOSE: {socket} {socket.State}");
			if (socket.State == WebSocketState.Open)
				await socket.CloseOutputAsync (WebSocketCloseStatus.NormalClosure, "Closing", cancellationToken);
			Log ($"CLOSE #1: {socket} {socket.State}");
			await base.Close (wait, cancellationToken).ConfigureAwait (false);
			Log ($"CLOSE #2: {socket} {socket.State}");
		}

		async Task Send (Command command, CancellationToken token)
		{
			var str = command.Encoded.ToString ();
			var bytes = Encoding.UTF8.GetBytes (str);

			await socket.SendAsync (new ArraySegment<byte> (bytes), WebSocketMessageType.Text, true, token).ConfigureAwait (false);
			LogProtocol (command.Method, "SEND");
		}

		protected abstract JObject Encode (Command command);

		async void MainLoop (CancellationToken token)
		{
			while (!token.IsCancellationRequested) {
				try {
					var result = await ReadOne (token).ConfigureAwait (false);
					if (!result)
						return;
				} catch (TaskCanceledException) {
					return;
				} catch (WebSocketException ex) {
					if (ex.WebSocketErrorCode == WebSocketError.ConnectionClosedPrematurely)
						return;
					throw;
				}
			}
		}
	}
}
