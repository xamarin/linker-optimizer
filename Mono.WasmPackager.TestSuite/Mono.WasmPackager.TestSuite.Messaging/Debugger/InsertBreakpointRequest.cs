namespace Mono.WasmPackager.TestSuite.Messaging.Debugger
{
	public class InsertBreakpointRequest : ProtocolObject
	{
		public string Url { get; set; }
		public int LineNumber { get; set; }
		public int ColumnNumber { get; set; }
	}
}
