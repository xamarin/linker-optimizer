using Newtonsoft.Json.Linq;

namespace Mono.WasmPackager.TestSuite.Messaging.Debugger
{
	public class PausedNotification : ProtocolObject
	{
		public CallFrame[] CallFrames { get; set; }
		public StoppedReason Reason { get; set; }
		public string[] HitBreakpoints { get; set; }
		public JObject Data { get; set; }
	}
}
