using System;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Mono.WasmPackager.DevServer
{
	public class ConnectionEventArgs
	{
		public string SessionId {
			get; internal set;
		}

		public int Id {
			get; internal set;
		}

		public string Message {
			get; internal set;
		}

		public JObject Arguments {
			get; internal set;
		}

		public bool Close {
			get; internal set;
		}

		public Func<CancellationToken, Task> Handler {
			get; internal set;
		}

		public string Sender {
			get; internal set;
		}

		public override string ToString () => $"[{GetType ().Name}: {Sender} {SessionId} {Message}]";
	}
}
