using System;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.StaticFiles;
using System.Text.Encodings.Web;

namespace Mono.WasmPackager.DevServer
{
	public class ServerMiddleware
	{
		ServerOptions DevServerOptions {
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

		public ServerMiddleware (RequestDelegate next, IWebHostEnvironment env, IOptions<ServerOptions> options, ILoggerFactory loggerFactory)
		{
			Next = next;
			DevServerOptions = options.Value;
			Logger = loggerFactory.CreateLogger<ServerMiddleware> ();

			var chain = next;

			if (DevServerOptions.FileServerOptions.EnableDirectoryBrowsing) {
				var dbo = DevServerOptions.FileServerOptions.DirectoryBrowserOptions;
				DirectoryBrowser = new DirectoryBrowserMiddleware (chain, env, HtmlEncoder.Default, Options.Create (dbo));
				chain = DirectoryBrowser.Invoke;
			}

			var contentTypeProvider = new FileExtensionContentTypeProvider ();
			AddMapping (contentTypeProvider, ".dll", MediaTypeNames.Application.Octet);
			AddMapping (contentTypeProvider, ".exe", MediaTypeNames.Application.Octet);
			if (DevServerOptions.EnableDebugging)
				AddMapping (contentTypeProvider, ".pdb", MediaTypeNames.Application.Octet);

			AddStaticFileMiddleware (null, DevServerOptions.WebRoot);
			if (DevServerOptions.FrameworkDirectory != null)
				AddStaticFileMiddleware ("/_framework", DevServerOptions.FrameworkDirectory);

			Chain = chain;

			void AddStaticFileMiddleware (string root, string path)
			{
				var sfo = new StaticFileOptions
				{
					ContentTypeProvider = contentTypeProvider,
					RequestPath = root,
					FileProvider = new PhysicalFileProvider (path),
					ServeUnknownFileTypes = true
				};
				var handler = new StaticFileMiddleware (chain, env, Options.Create (sfo), loggerFactory);
				chain = handler.Invoke;
			}
		}

		static void AddMapping (FileExtensionContentTypeProvider provider, string name, string mimeType)
		{
			if (!provider.Mappings.ContainsKey (name))
				provider.Mappings.Add (name, mimeType);
		}

		public Task Invoke (HttpContext context)
		{
			Logger.Log (LogLevel.Information, $"INVOKE: {context.Connection.LocalPort} {context.Request.Method} {context.Request.Path}");
			if (context.Connection.LocalPort != DevServerOptions.FileServerPort)
				return Next (context);
			if (!HttpMethods.IsGet (context.Request.Method) && !HttpMethods.IsHead (context.Request.Method))
				return Next (context);

			return Chain (context);
		}

		Task TryServeStaticFile (HttpContext context)
		{
			throw new NotImplementedException ();
		}

	}
}