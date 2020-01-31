using System;
using System.Linq;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Routing;

namespace Mono.WasmPackager.DevServer
{
	class DevServer
	{
		const string DEFAULT_ROOT = "/Users/Workspace/test-optimizer/Tests/Packager/SimpleWeb/app";

		static void Main (string[] args)
		{
			string root = DEFAULT_ROOT;
			string framework = null;
			string blazor;

			int pos = 0;
			while (pos < args.Length) {
				var key = args[pos++];
				switch (key) {
				case "--web-root":
					root = args[pos++];
					break;
				case "--framework":
					framework = args[pos++];
					break;
				case "--blazor":
					blazor = args[pos++];
					break;
				default:
					throw new NotSupportedException ($"Unknown command-line argument: '{key}'.");
				}
			}

			var host = new WebHostBuilder ()
				.UseKestrel ()
				.UseContentRoot (root)
				.UseWebRoot (root)
				.UseIISIntegration ()
				.UseStartup<DevServer> ()
				.ConfigureServices (services => {
					services.AddRouting ();
					services.Configure<DevServerOptions> (options => {
						options.WebRoot = root;
						options.FrameworkDirectory = framework;
						options.FileServerOptions.EnableDirectoryBrowsing = true;
						options.FileServerOptions.StaticFileOptions.ServeUnknownFileTypes = true;
					});
				})
				.Build ();

			host.Run ();
		}

		public DevServer (IConfiguration configuration) =>
			Configuration = configuration;

		public IConfiguration Configuration { get; }

		public void Configure (IApplicationBuilder app, IOptionsMonitor<DevServerOptions> optionsAccessor, IWebHostEnvironment env)
		{
			var options = optionsAccessor.CurrentValue;

			var addresses = app.ServerFeatures.Get<IServerAddressesFeature> ();
			addresses.Addresses.Add ($"http://localhost:{options.DebugServerPort}/");
			addresses.Addresses.Add ($"http://localhost:{options.FileServerPort}/");

			app.UseWebSockets ();
			app.UseDeveloperExceptionPage ();

			app.UseRouting ();

			var proxy = new DebugProxy (options);

			app.UseEndpoints (endpoints =>
			{
				proxy.ConfigureRoutes ((pattern, action) =>
					endpoints.MapGet (pattern, action).RequireHost ("*:9300"));
			});

			app.UseMiddleware<DevServerMiddleware> (Options.Create (options));
		}
	}
}
