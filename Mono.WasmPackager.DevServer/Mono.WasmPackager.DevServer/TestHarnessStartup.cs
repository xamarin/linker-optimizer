using System;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.StaticFiles;
using WebAssembly.Net.Debugging;
using Newtonsoft.Json.Linq;
using PuppeteerSharp;

namespace Mono.WasmPackager.DevServer
{
	public class TestHarnessStartup
	{
		public Server Server {
			get;
		}

		public static ConcurrentDictionary<string, CDPSession> Registration {
			get;
		} = new ConcurrentDictionary<string, CDPSession> ();

		static readonly TimeSpan StartupTimeout = TimeSpan.FromDays (10);

		public ServerOptions Options => Server.ServerOptions;

		public TestHarnessStartup (Server server)
		{
			Server = server;
		}

		public async Task ServePuppeteer (HttpContext context, CDPSession instance)
		{
			if (!context.WebSockets.IsWebSocketRequest) {
				context.Response.StatusCode = 400;
				return;
			}

			var tcs = new TaskCompletionSource<string> ();

			try {
				var proxy = new NewMonoProxy ();
				var connection = new PuppeteerConnection (instance);
//				await connection.Start (CancellationToken.None);

				var ideConnection = new ServerWebSocketConnection (context.WebSockets, instance.SessionId);
//				await ideConnection.Start (CancellationToken.None);

				await proxy.Run (connection, ideConnection);

				Debug.WriteLine ("Proxy done");
			} catch (Exception e) {
				Debug.WriteLine ("got exception {0}", e);
			}
		}

		async Task<string> GetPageUrl (HttpClient client, string str)
		{
			string res = null;
			var start = DateTime.Now;

			while (res == null) {
				// Unfortunately it does look like we have to wait
				// for a bit after getting the response but before
				// making the list request.  We get an empty result
				// if we make the request too soon.
				await Task.Delay (100);

				res = await client.GetStringAsync (new Uri (new Uri (str), "/json/list"));
				Debug.WriteLine ($"res is {res}");

				var elapsed = DateTime.Now - start;
				if (res == null && elapsed.Milliseconds > 2000) {
					Debug.WriteLine ($"Unable to get DevTools /json/list response in {elapsed.Seconds} seconds, stopping");
					return null;
				}
			}

			var obj = JArray.Parse (res);
			if (obj == null || obj.Count < 1)
				return null;

			var wsURl = obj [0]? ["webSocketDebuggerUrl"]?.Value<string> ();
			Debug.WriteLine ($">>> {wsURl}");

			return wsURl;
		}

		public void Configure (IEndpointRouteBuilder router)
		{
			router.MapGet ("connect-to-puppeteer", async context => {
				var instanceId = context.Request.Query ["instance-id"];
				Debug.WriteLine ($"New puppeteer instance test request: {instanceId}");
				if (!Registration.TryGetValue (instanceId, out var session)) {
					context.Response.StatusCode = 400;
					return;
				}
				await ServePuppeteer (context, session);
			});
		}
	}
}
