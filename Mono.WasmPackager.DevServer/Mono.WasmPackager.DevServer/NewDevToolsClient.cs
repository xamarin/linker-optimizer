using System;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using WebAssembly.Net.Debugging;

namespace Mono.WasmPackager.DevServer
{
	public class NewDevToolsClient : IDisposable
	{
		readonly AbstractConnection connection;
		int disposed;

		public NewDevToolsClient (AbstractConnection connection)
		{
			this.connection = connection;
		}

		public AbstractConnection Connection => connection;

		public void Dispose ()
		{
			if (Interlocked.CompareExchange (ref disposed, 1, 0) == 0)
				DoDispose ();
		}

		public Task Close (bool wait, CancellationToken cancellationToken)
		{
			return connection.Close (wait, cancellationToken);
		}

		protected virtual void DoDispose ()
		{
			connection.Dispose ();
		}

		public Task<JObject> SendCommand (string method, JObject args, CancellationToken token)
		{
			return connection.SendAsync (default (SessionId), method, args);
		}
	}
}