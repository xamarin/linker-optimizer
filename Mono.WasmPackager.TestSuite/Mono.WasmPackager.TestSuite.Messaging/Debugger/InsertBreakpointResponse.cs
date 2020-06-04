namespace Mono.WasmPackager.TestSuite.Messaging.Debugger
{
	public class InsertBreakpointResponse : ProtocolObject
	{
		public string BreakpointId { get; set; }
		public Location[] Locations { get; set; }
	}
}
