using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
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
	public class Server : StartupBase, IDisposable
	{
		const string DEFAULT_ROOT = "/Users/Workspace/test-optimizer/Tests/Packager/SimpleWeb/app";

		static void Main (string[] args)
		{
			string root = DEFAULT_ROOT;
			string framework = null;
			bool debug = false;

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
					case "--debug":
						debug = true;
						break;
					default:
						throw new NotSupportedException ($"Unknown command-line argument: '{key}'.");
				}
			}

			var options = new ServerOptions
			{
				WebRoot = root,
				EnableDebugging = debug,
				FrameworkDirectory = framework
			};

			options.FileServerOptions.EnableDirectoryBrowsing = true;
			options.FileServerOptions.StaticFileOptions.ServeUnknownFileTypes = true;

			var server = new Server (options);
			server.Host.Run ();
		}

		public static Server CreateTestHarness (string root, string chrome)
		{
			if (string.IsNullOrEmpty (root))
				throw new ArgumentNullException (nameof (root));
			if (string.IsNullOrEmpty (chrome))
				throw new ArgumentNullException (nameof (chrome));

			var options = new ServerOptions
			{
				WebRoot = root,
				ChromePath = chrome,
				EnableDebugging = true,
				EnableTestHarness = true
			};

			options.FileServerOptions.EnableDirectoryBrowsing = true;
			options.FileServerOptions.StaticFileOptions.ServeUnknownFileTypes = true;

			return new Server (options);
		}

		public Server (ServerOptions options)
		{
			ServerOptions = options;

			var builder = new WebHostBuilder ()
				.UseKestrel ()
				.UseContentRoot (options.WebRoot)
				.UseWebRoot (options.WebRoot)
				.UseIISIntegration ()
				.ConfigureLogging (logging =>
				{
					logging.AddConsole ();
					logging.AddDebug ();
				})
				.ConfigureServices (services =>
				{
					services.AddSingleton (typeof (IStartup), this);
				});

			Host = builder.Build ();
		}

		public IWebHost Host { get; }

		public ServerOptions ServerOptions { get; private set; }

		public DebugProxy Proxy => Host.Services.GetRequiredService<DebugProxy> ();

		public override void ConfigureServices (IServiceCollection services)
		{
			services.AddSingleton (this);
			services.AddSingleton (ServerOptions);

			services.AddSingleton (new DebugProxy (ServerOptions));

			if (ServerOptions.EnableTestHarness)
				services.AddSingleton (new TestHarnessStartup (this));

			services.AddRouting ();
		}

		public override void Configure (IApplicationBuilder app)
		{
			var addresses = app.ServerFeatures.Get<IServerAddressesFeature> ();
			addresses.Addresses.Add ($"http://localhost:{ServerOptions.DebugServerPort}/");
			addresses.Addresses.Add ($"http://localhost:{ServerOptions.FileServerPort}/");

			app.UseWebSockets ();
			app.UseDeveloperExceptionPage ();

			app.UseRouting ();

			var proxy = app.ApplicationServices.GetRequiredService<DebugProxy> ();

			app.UseEndpoints (endpoints =>
			{
				proxy.ConfigureRoutes ((pattern, action) =>
					endpoints.MapGet (pattern, action).RequireHost ("*:9300"));
				if (ServerOptions.EnableTestHarness) {
					var harness = app.ApplicationServices.GetRequiredService<TestHarnessStartup> ();
					harness.Configure (endpoints);
				}
			});

			app.UseMiddleware<ServerMiddleware> (Options.Create (ServerOptions));
		}

		public void Dispose ()
		{
			Host.Dispose ();
		}
	}
}
