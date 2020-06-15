namespace Mono.WasmPackager.TestSuite.Messaging.Debugger
{
	public class InsertBreakpointRequest : ProtocolRequest<InsertBreakpointResponse>
	{
		public override string Command => "Debugger.setBreakpointByUrl";

		public string Url { get; set; }
		public int LineNumber { get; set; }
		public int ColumnNumber { get; set; }
	}
}
