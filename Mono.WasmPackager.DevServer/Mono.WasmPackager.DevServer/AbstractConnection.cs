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
using WebAssembly.Net.Debugging;

namespace Mono.WasmPackager.DevServer
{
	public abstract class AbstractConnection : IDisposable
	{
		public string SessionId {
			get;
			protected set;
		}

		public string Name {
			get;
		}

		readonly AsyncQueue<ConnectionEventArgs> asyncQueue;
		int running;
		int disposed;

		public bool IsRunning => running != 0;

		protected AbstractConnection (string sessionId, string name)
		{
			SessionId = sessionId;
			Name = name;

			asyncQueue = new AsyncQueue<ConnectionEventArgs> (false, $"{Name}:main");
		}

		protected Task OnEvent (ConnectionEventArgs args)
		{
			return asyncQueue.EnqueueAsync (args, args.Close);
		}

		protected void OnEventSync (ConnectionEventArgs args)
		{
			asyncQueue.Enqueue (args, args.Close);
		}

		public async Task Start (Func<ConnectionEventArgs, CancellationToken, Task> handler, CancellationToken token)
		{
			if (Interlocked.CompareExchange (ref running, 1, 0) != 0)
				return;

			asyncQueue.Start (handler);
			await Start (token);
		}

		protected abstract Task Start (CancellationToken token);

		public virtual async Task Close (bool wait, CancellationToken cancellationToken)
		{
			if (wait)
				await asyncQueue.Close ();
		}

		internal abstract Task<JObject> SendAsync (SessionId sessionId, string method, object args = null, bool waitForCallback = true);

		public override string ToString () => $"[{GetType ().Name}:{Name}]";

		public void Dispose ()
		{
			if (Interlocked.CompareExchange (ref disposed, 1, 0) == 0)
				DoDispose ();
		}

		protected virtual void DoDispose ()
		{
			asyncQueue.Dispose ();
		}
	}
}
