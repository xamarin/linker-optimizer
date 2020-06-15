using WebAssembly.Net.Debugging;

namespace Mono.WasmPackager.DevServer {
	struct CommandResult {
		public Result Result {
			get;
		}

		public bool HasResult {
			get;
		}

		public bool ProxyCommand {
			get;
		}

		private CommandResult (Result result)
		{
			Result = result;
			HasResult = true;
			ProxyCommand = false;
		}

		private CommandResult (bool proxy)
		{
			Result = default;
			HasResult = false;
			ProxyCommand = proxy;
		}

		public static readonly CommandResult Ignore = new CommandResult (false);

		public static readonly CommandResult Proxy = new CommandResult (true);

		public static implicit operator CommandResult (Result result) => new CommandResult (result);
	}
}
