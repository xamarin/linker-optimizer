using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Collections.Generic;
using System.Collections.Concurrent;
using Newtonsoft.Json.Linq;

namespace Mono.WasmPackager.DevServer
{
	public class ClientWebSocketConnection : WebSocketConnection
	{
		public Uri Uri {
			get;
		}

		public ClientWebSocketConnection (Uri uri, string sessionId, string name = null)
			: base (sessionId, name ?? "client")
		{
			Uri = uri;
		}

		protected override async Task<WebSocket> CreateSocket (CancellationToken token)
		{
			var socket = new ClientWebSocket ();
			socket.Options.KeepAliveInterval = Timeout.InfiniteTimeSpan;

			await socket.ConnectAsync (Uri, token);
			return socket;
		}

		protected override ConnectionEventArgs Decode (JObject message)
		{
			if (message ["id"] == null)
				return new ConnectionEventArgs {
					Sender = Name,
					SessionId = SessionId,
					Message = message ["method"].Value<string> (),
					Arguments = message ["params"] as JObject
				};

			var id = message ["id"].Value<int> ();
			var idx = pendingCmds.FindIndex (e => e.Item1 == id);
			DumpProtocol ($"ON MESSAGE: {id} {idx}");
			var command = pendingCmds [idx];
			pendingCmds.RemoveAt (idx);
			command.Item2.Completion.SetResult (message);
			return null;
		}

		protected override JObject Encode (Command command)
		{
			int id = ++next_cmd_id;
			var args = command.Arguments ?? new JObject ();

			DumpProtocol ($"ENCODE: {id} {command.Method}");
			pendingCmds.Add ((id, command));

			var obj = JObject.FromObject (new {
				id = id,
				method = command.Method,
				@params = args
			});

			if (command.SessionId != null)
				obj["sessionId"] = command.SessionId;

			return obj;
		}
	}
}
