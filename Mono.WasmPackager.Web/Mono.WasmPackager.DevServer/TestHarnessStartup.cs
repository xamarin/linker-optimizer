using System.Diagnostics;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Mono.WasmPackager.DevServer
{
	public class TestHarnessStartup
	{
		public Server Server {
			get;
		}

		public static ConcurrentDictionary<string, TestSession> Registration {
			get;
		} = new ConcurrentDictionary<string, TestSession> ();

		public ServerOptions Options => Server.ServerOptions;

		public TestHarnessStartup (Server server)
		{
			Server = server;
		}

		public async Task ServePuppeteer (HttpContext context, TestSession instance)
		{
			if (!context.WebSockets.IsWebSocketRequest) {
				context.Response.StatusCode = 400;
				return;
			}

			using var proxy = NewDevToolsProxy.Create (instance, context.WebSockets);
			await proxy.Start ().ConfigureAwait (false);

			await proxy.WaitForExit ().ConfigureAwait (false);

			Debug.WriteLine ("Proxy done");
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
				Debug.WriteLine ($"Puppeteer instance test request done: {instanceId}");
				await context.Response.CompleteAsync ();
			});
		}
	}
}
