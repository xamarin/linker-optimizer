using Newtonsoft.Json.Linq;
using Mono.WasmPackager.TestSuite.Messaging.Runtime;

namespace Mono.WasmPackager.TestSuite.Messaging.Debugger
{
	// Unofficial, not part of the DevTools Protocol
	public class PausedExceptionData : ProtocolObject
	{
		public RemoteObjectType Type { get; set; }
		public RemoteObjectSubType SubType { get; set; }
		public string ClassName { get; set; }
		public string Description { get; set; }
		public string ObjectId { get; set; }
		public bool Uncaught { get; set; }
	}
}
