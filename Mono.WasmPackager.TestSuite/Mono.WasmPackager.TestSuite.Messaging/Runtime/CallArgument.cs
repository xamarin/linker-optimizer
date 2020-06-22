using Newtonsoft.Json.Linq;

namespace Mono.WasmPackager.TestSuite.Messaging.Runtime
{
	public class CallArgument : ProtocolObject
	{
		public JObject Value { get; set; }
		public string UnserializableValue { get; set; }
		public string ObjectId { get; set; }
	}
}
