using System;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

using Mono.WasmPackager.DevServer;

namespace WebAssembly.Net.Debugging {
	struct CommandResult {
		public Result Result {
			get;
		}

		public bool HasResult {
			get;
		}

		public bool IgnoreCommand {
			get;
		}

		public Func<CancellationToken, Task> Handler {
			get;
		}

		private CommandResult (Func<CancellationToken, Task> handler, bool ignore)
		{
			Result = default (Result);
			HasResult = false;
			Handler = handler;
			IgnoreCommand = ignore;
		}

		private CommandResult (Result result, bool ignore)
		{
			Result = result;
			HasResult = true;
			Handler = null;
			IgnoreCommand = ignore;
		}

		public static CommandResult Complete = new CommandResult (null, true);

		public static CommandResult Proxy = new CommandResult (null, false);

		public static CommandResult Async (Func<CancellationToken, Task> handler) => new CommandResult (handler, false);

		public static CommandResult XAsync (MessageId id, string method, JObject args, ExecutionContext context, Func<MessageId, string, JObject, ExecutionContext, CancellationToken, Task> func)
		{
			return Async (token => func (id, method, args, context, token));
		}

		public static CommandResult FromResult (Result result) => new CommandResult (result, false);
	}
}
