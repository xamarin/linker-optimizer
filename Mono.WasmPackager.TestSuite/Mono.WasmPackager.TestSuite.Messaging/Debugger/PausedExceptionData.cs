// using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Mono.WasmPackager.TestSuite.Messaging.Debugger
{
	// Unofficial, not part of the DevTools Protocol
	public class PausedExceptionData : ProtocolObject
	{
		public string Type { get; set; }
		public string Subtype { get; set; }
		public string ClassName { get; set; }
		public string Description { get; set; }
		public string ObjectId { get; set; }
		public bool Uncaught { get; set; }
	}
}
