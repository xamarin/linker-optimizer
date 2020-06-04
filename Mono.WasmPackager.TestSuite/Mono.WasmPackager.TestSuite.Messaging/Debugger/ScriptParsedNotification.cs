using Newtonsoft.Json.Linq;

namespace Mono.WasmPackager.TestSuite.Messaging.Debugger
{
	public class ScriptParsedNotification : ProtocolObject
	{
		public string ScriptId { get; set; }
		public string Url { get; set; }
		public int StartLine { get; set; }
		public int StartColumn { get; set; }
		public int EndLine { get; set; }
		public int EndColumn { get; set; }
		public int ExceptionContextId { get; set; }
		public string Hash { get; set; }
		// Everything below is optional
		public bool HasSourceURL { get; set; }
		public bool IsModule { get; set; }
		public int Length { get; set; }
		// Mono addition
		public string DotNetUrl { get; set; }
	}
}
