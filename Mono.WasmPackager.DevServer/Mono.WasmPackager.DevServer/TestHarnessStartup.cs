using System;
using System.Diagnostics;
using System.IO;
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

namespace Mono.WasmPackager.DevServer
{
	public class TestHarnessStartup
	{
		public Server Server {
			get;
		}

		static readonly TimeSpan StartupTimeout = TimeSpan.FromDays (10);

		public ServerOptions Options => Server.ServerOptions;

		public TestHarnessStartup (Server server)
		{
			Server = server;
		}

		public async Task LaunchAndServe (HttpContext context, Func<string, Task<string>> extract_conn_url)
		{
			if (!context.WebSockets.IsWebSocketRequest) {
				context.Response.StatusCode = 400;
				return;
			}

			var tcs = new TaskCompletionSource<string> ();

			var headless = Options.Headless ? "--headless " : string.Empty;

			var psi = new ProcessStartInfo
			{
				Arguments = $"{headless} -incognito --disable-gpu --remote-debugging-port={Options.DevToolsUrl.Port} http://localhost:{Options.FileServerPort}/{Options.PagePath}",
				UseShellExecute = false,
				FileName = Options.ChromePath,
				RedirectStandardError = true,
				RedirectStandardOutput = true
			};

			var proc = Process.Start (psi);
			try {
				proc.ErrorDataReceived += (sender, e) =>
				{
					var str = e.Data;
					if (string.IsNullOrEmpty (str))
						return;
					Debug.WriteLine ($"stderr: {str}");

					if (str.Contains ("listening on", StringComparison.Ordinal)) {
						var res = str.Substring (str.IndexOf ("ws://", StringComparison.Ordinal));
						if (res != null)
							tcs.TrySetResult (res);
					}
				};

				proc.OutputDataReceived += (sender, e) =>
				{
					Debug.WriteLine ($"stdout: {e.Data}");
				};

				proc.BeginErrorReadLine ();
				proc.BeginOutputReadLine ();

				if (await Task.WhenAny (tcs.Task, Task.Delay (StartupTimeout)) != tcs.Task) {
					Debug.WriteLine ("Didnt get the con string after 2s.");
					throw new Exception ("node.js timedout");
				}
				var line = await tcs.Task;
				var con_str = extract_conn_url != null ? await extract_conn_url (line) : line;

				Debug.WriteLine ($"lauching proxy for {con_str}");

				var proxy = new MonoProxy ();
				var browserUri = new Uri (con_str);
				var ideSocket = await context.WebSockets.AcceptWebSocketAsync ();

				await proxy.Run (browserUri, ideSocket);
				Debug.WriteLine ("Proxy done");
			} catch (Exception e) {
				Debug.WriteLine ("got exception {0}", e);
			} finally {
				proc.CancelErrorRead ();
				proc.CancelOutputRead ();
				proc.Kill ();
				proc.WaitForExit ();
				proc.Close ();
			}
		}

		public async Task ServePuppeteer (HttpContext context, string puppeteerUrl)
		{
			if (!context.WebSockets.IsWebSocketRequest) {
				context.Response.StatusCode = 400;
				return;
			}

			var tcs = new TaskCompletionSource<string> ();

			try {
				var proxy = new MonoProxy ();
				var browserUri = new Uri (puppeteerUrl);
				var ideSocket = await context.WebSockets.AcceptWebSocketAsync ();

				await proxy.Run (browserUri, ideSocket);
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

			var wsURl = obj[0]?["webSocketDebuggerUrl"]?.Value<string> ();
			Debug.WriteLine ($">>> {wsURl}");

			return wsURl;
		}

		public void Configure (IEndpointRouteBuilder router)
		{
			router.MapGet ("launch-chrome-and-connect", async context =>
			{
				Debug.WriteLine ("New test request");
				var client = new HttpClient ();
				await LaunchAndServe (context, str => GetPageUrl (client, str));
			});
			router.MapGet ("connect-to-puppeteer", async context =>
			{
				var puppeteerPort = context.Request.Query["puppeteer-port"];
				var pageId = context.Request.Query["page-id"];
				Debug.WriteLine ($"New test request: {puppeteerPort} {pageId}");
				var pageUrl = $"ws://127.0.0.1:{puppeteerPort}/devtools/page/{pageId}";
				await ServePuppeteer (context, pageUrl);
			});
		}
	}
}
