using System.Diagnostics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Mono.WasmPackager.TestSuite.Messaging
{
	[JsonConverter (typeof (ProtocolObjectConverter))]
	public abstract class ProtocolObject
	{
		[JsonIgnore]
		[DebuggerBrowsable (DebuggerBrowsableState.Never)]
		public JToken OriginalJToken { get; set; }
	}
}
