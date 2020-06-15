namespace Mono.WasmPackager.TestSuite.Messaging.Debugger
{
	public class GetPossibleBreakpointsResponse : ProtocolResponse
	{
		public Location[] Locations { get; set; }
	}
}
