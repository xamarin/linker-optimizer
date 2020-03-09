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
using PuppeteerSharp;

namespace Mono.WasmPackager.DevServer
{
	public class PuppeteerConnection : AbstractConnection
	{
		public CDPSession Session {
			get;
		}

		internal PuppeteerConnection (CDPSession session)
			: base (session.SessionId, session.SessionId)
		{
			Session = session;
		}

		protected override Task Start (CancellationToken token)
		{
			Session.MessageReceived += (sender, e) => OnEventSync (new ConnectionEventArgs {
				Sender = Name,
				SessionId = SessionId,
				Message = e.MessageID,
				Arguments = (JObject)e.MessageData
			});
			return Task.CompletedTask;
		}

		public override Task Close (CancellationToken cancellationToken) => Task.CompletedTask;

		public override Task<JObject> SendAsync (SessionId sessionId, string method, object args = null, bool waitForCallback = true)
		{
			return Session.SendAsync (method, args, waitForCallback);
		}
	}
}
