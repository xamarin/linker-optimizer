using System;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace WebAssembly.Net.Debugging {
	interface IMonoProxy {
		Task SendEvent (SessionId sessionId, string method, JObject args, CancellationToken token);
	}
}
