using System;
using System.Threading;
using System.Threading.Tasks;
using System.Net.WebSockets;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;

namespace Mono.WasmPackager.DevServer
{
	public class ServerWebSocketConnection : WebSocketConnection
	{
		public ServerWebSocketConnection (WebSocketManager manager, string sessionId, string name = null)
			: base (sessionId, name ?? "server")
		{
			this.manager = manager;
		}

		readonly WebSocketManager manager;

		protected override Task<WebSocket> CreateSocket (CancellationToken token)
		{
			return manager.AcceptWebSocketAsync ();
		}

		protected override ConnectionEventArgs Decode (JObject message)
		{
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
			} else {
				id = ++next_cmd_id;
				o = JObject.FromObject (new {
					method = command.Method,
					@params = args
				});
			}

			pendingCmds.Add ((id, command));

			return o;
		}
	}
}
