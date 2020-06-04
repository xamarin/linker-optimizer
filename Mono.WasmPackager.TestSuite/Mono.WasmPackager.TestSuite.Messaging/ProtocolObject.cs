using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Mono.WasmPackager.TestSuite.Messaging
{
	[JsonConverter (typeof (ProtocolObjectConverter))]
	public abstract class ProtocolObject
	{
		[JsonIgnore]
		public JToken OriginalJToken { get; set; }
	}
}
