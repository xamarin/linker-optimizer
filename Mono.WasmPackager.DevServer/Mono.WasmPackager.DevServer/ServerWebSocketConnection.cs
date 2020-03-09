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
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;
using WebAssembly.Net.Debugging;

namespace Mono.WasmPackager.DevServer
{
	public class ServerWebSocketConnection : WebSocketConnection
	{
		public ServerWebSocketConnection (WebSocketManager manager, string sessionId, string name = null)
			: base (sessionId, name ?? "server")
		{
			this.manager = manager;
		}

		WebSocketManager manager;

		protected override Task<WebSocket> CreateSocket (CancellationToken token)
		{
			return manager.AcceptWebSocketAsync ();
		}

		protected override ConnectionEventArgs Decode (JObject message)
		{
			DumpProtocol ($"DECODE: {message}");
			var args = new ConnectionEventArgs {
				Sender = Name,
				SessionId = SessionId,
				Message = message ["method"].Value<string> (),
				Arguments = message ["params"] as JObject
			};
			if (message ["id"] != null)
				args.Id = message ["id"].Value<int> ();
			else
				throw new NotSupportedException ();

			return args;
		}

		protected override JObject Encode (Command command)
		{
			int id;
			var args = (JObject)command.Arguments ?? new JObject ();

			JObject o;
			if (command.Method == null && args["id"] != null) {
				id = args["id"].Value<int> ();
				o = (JObject)command.Arguments;
				/*
				o = JObject.FromObject (new {
					id = id,
					@params = args
				});
				*/
			} else {
				id = ++next_cmd_id;
				o = JObject.FromObject (new {
					method = command.Method,
					@params = args
				});
			}

			DumpProtocol ($"ENCODE: {id} {command.Method}");
			pendingCmds.Add ((id, command));

			return o;
		}
	}
}
