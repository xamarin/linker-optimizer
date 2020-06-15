using System;
using System.Threading;
using System.Threading.Tasks;

using WebAssembly.Net.Debugging;

namespace Mono.WasmPackager.DevServer {
	struct ProxyCommand {
		public CommandResult Result {
			get;
		}

		public bool HasResult => Handler == null;

		public Func<CancellationToken, ValueTask<CommandResult>> Handler {
			get;
		}

		private ProxyCommand (Func<CancellationToken, ValueTask<CommandResult>> handler)
		{
			Handler = handler;
			Result = default;
		}

		private ProxyCommand (CommandResult result)
		{
			Handler = null;
			Result = result;
		}

		public static ProxyCommand Complete = new ProxyCommand (CommandResult.Ignore);

		public static ProxyCommand Proxy = new ProxyCommand (CommandResult.Proxy);

		public static ProxyCommand Async (Func<CancellationToken, Task<Result>> handler) => new ProxyCommand (async token => {
			return await handler (token).ConfigureAwait (false);
		});

		public static ProxyCommand AsyncProxy (Func<CancellationToken, Task> handler) => new ProxyCommand (async token => {
			await handler (token).ConfigureAwait (false);
			return CommandResult.Proxy;
		});

		public static ProxyCommand AsyncBool (Func<CancellationToken, Task<bool>> handler) => new ProxyCommand (async token => {
			var complete = await handler (token).ConfigureAwait (false);
			return complete ? CommandResult.Ignore : CommandResult.Proxy;
		});

		public static ProxyCommand Async (Func<CancellationToken, ValueTask<CommandResult>> handler) => new ProxyCommand (handler);
	}
}
