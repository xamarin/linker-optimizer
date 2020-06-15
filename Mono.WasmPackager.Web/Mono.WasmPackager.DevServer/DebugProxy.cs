using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace Mono.WasmPackager.DevServer
{
	public class DebugProxy
	{
		public ServerOptions Options {
			get;
		}

		public Uri DevToolsHost => Options.DevToolsUrl;

		public DebugProxy (ServerOptions options)
		{
			Options = options;
		}

		static Dictionary<string, string> MapValues (Dictionary<string, string> response, HttpContext context, Uri debuggerHost)
		{
			var filtered = new Dictionary<string, string> ();
			var request = context.Request;

			foreach (var key in response.Keys) {
				switch (key) {
				case "devtoolsFrontendUrl":
					var front = response [key];
					filtered [key] = $"{debuggerHost.Scheme}://{debuggerHost.Authority}{front.Replace ($"ws={debuggerHost.Authority}", $"ws={request.Host}")}";
					break;
				case "webSocketDebuggerUrl":
					var page = new Uri (response [key]);
					filtered [key] = $"{page.Scheme}://{request.Host}{page.PathAndQuery}";
					break;
				default:
					filtered [key] = response [key];
					break;
				}
			}
			return filtered;
		}

		string GetEndpoint (HttpContext context)
		{
			var request = context.Request;
			return $"{DevToolsHost.Scheme}://{DevToolsHost.Authority}{request.Path}{request.QueryString}";
		}

		async Task Copy (HttpContext context)
		{
			using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds (5) };
			var response = await httpClient.GetAsync (GetEndpoint (context));
			context.Response.ContentType = response.Content.Headers.ContentType.ToString ();
			if ((response.Content.Headers.ContentLength ?? 0) > 0)
				context.Response.ContentLength = response.Content.Headers.ContentLength;
			var bytes = await response.Content.ReadAsByteArrayAsync ();
			await context.Response.Body.WriteAsync (bytes);
		}

		async Task RewriteSingle (HttpContext context)
		{
			var version = await ProxyGetJsonAsync<Dictionary<string, string>> (GetEndpoint (context));
			context.Response.ContentType = "application/json";
			await context.Response.WriteAsync (
				JsonConvert.SerializeObject (MapValues (version, context, DevToolsHost)));
		}

		async Task RewriteArray (HttpContext context)
		{
			var tabs = await ProxyGetJsonAsync<Dictionary<string, string> []> (GetEndpoint (context));
			var alteredTabs = tabs.Select (t => MapValues (t, context, DevToolsHost)).ToArray ();
			context.Response.ContentType = "application/json";
			await context.Response.WriteAsync (JsonConvert.SerializeObject (alteredTabs));
		}

		async Task HandleDevToolsPage (HttpContext context)
		{
			if (!context.WebSockets.IsWebSocketRequest) {
				context.Response.StatusCode = 400;
				return;
			}

			var endpoint = new Uri ($"ws://{DevToolsHost.Authority}{context.Request.Path}");
			using var proxy = NewDevToolsProxy.Create (endpoint, context.WebSockets);
			await proxy.Start ().ConfigureAwait (false);

			await proxy.WaitForExit ().ConfigureAwait (false);
		}

		public void ConfigureRoutes (Action<string, RequestDelegate> mapGet)
		{
			mapGet ("/", Copy);
			mapGet ("/favicon.ico", Copy);
			mapGet ("json", RewriteArray);
			mapGet ("json/list", RewriteArray);
			mapGet ("json/version", RewriteSingle);
			mapGet ("json/new", RewriteSingle);
			mapGet ("devtools/page/{pageId}", HandleDevToolsPage);
		}

		static async Task<T> ProxyGetJsonAsync<T> (string url)
		{
			using var httpClient = new HttpClient ();
			var response = await httpClient.GetAsync (url);
			var jsonResponse = await response.Content.ReadAsStringAsync ();
			return JsonConvert.DeserializeObject<T> (jsonResponse);
		}
	}
}