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

		internal PuppeteerConnection (CDPSession session, string name = null)
			: base (session.SessionId, name)
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

		internal override async Task<JObject> SendAsync (SessionId sessionId, string method, object args = null, bool waitForCallback = true)
		{
			var obj = await Session.SendAsync (method, args, waitForCallback);
			return JObject.FromObject (new { result = obj });
		}
	}
}
