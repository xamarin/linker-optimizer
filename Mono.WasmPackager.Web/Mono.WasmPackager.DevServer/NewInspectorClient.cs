using System;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

using System.Net.WebSockets;
using System.Threading;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;
using WebAssembly.Net.Debugging;

namespace Mono.WasmPackager.DevServer
{
	public class NewInspectorClient : NewDevToolsClient
	{
		ClientWebSocketConnection connection;
		CancellationTokenSource cts = new CancellationTokenSource ();

		public NewInspectorClient (AbstractConnection connection)
			: base (connection)
		{
		}

		public async Task Connect (
			Uri uri,
			Func<ConnectionEventArgs, CancellationToken, Task> onEvent,
			Func<CancellationToken, Task> send,
			CancellationToken token)
		{
			connection = new ClientWebSocketConnection (uri, null);
			await connection.Start (onEvent, token).ConfigureAwait (false);

			await send (token).ConfigureAwait (false);

			connection.Dispose ();
		}
	}
}

