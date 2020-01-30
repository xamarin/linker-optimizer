using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.StaticFiles;
using System.Text.Encodings.Web;

namespace Mono.WasmPackager.DevServer
{
	public class DevServerMiddleware
	{
		DevServerOptions DevServerOptions {
			get;
		}

		RequestDelegate Next {
			get;
		}

		RequestDelegate Chain {
			get;
		}

		ILogger Logger {
			get;
		}

		DirectoryBrowserMiddleware DirectoryBrowser {
			get;
		}

		StaticFileMiddleware StaticFileHandler {
			get;
		}

		public DevServerMiddleware (RequestDelegate next, IWebHostEnvironment env, IOptions<DevServerOptions> options, ILoggerFactory loggerFactory)
		{
			Next = next;
			DevServerOptions = options.Value;
			Logger = loggerFactory.CreateLogger<DevServerMiddleware> ();

			Chain = Next;

			if (DevServerOptions.FileServerOptions.EnableDirectoryBrowsing) {
				var dbo = DevServerOptions.FileServerOptions.DirectoryBrowserOptions;
				DirectoryBrowser = new DirectoryBrowserMiddleware (Chain, env, HtmlEncoder.Default, Options.Create (dbo));
				Chain = DirectoryBrowser.Invoke;
			}

			var sfo = DevServerOptions.FileServerOptions.StaticFileOptions;
			StaticFileHandler = new StaticFileMiddleware (Chain, env, Options.Create (sfo), loggerFactory);
			Chain = StaticFileHandler.Invoke;
		}

		public Task Invoke (HttpContext context)
		{
			Logger.Log (LogLevel.Information, $"INVOKE: {context.Connection.LocalPort} {context.Request.Method} {context.Request.Path}");
			if (context.Connection.LocalPort != DevServerOptions.FileServerPort)
				return Next (context);
			return Chain (context);
		}
	}
}