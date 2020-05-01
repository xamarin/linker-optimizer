namespace Mono.WasmPackager.TestSuite.Messaging.Debugger
{
	public class InsertBreakpointResponse
	{
		public string BreakpointId { get; set; }
		public Location[] Locations { get; set; }
	}
}
