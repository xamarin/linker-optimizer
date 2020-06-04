namespace Mono.WasmPackager.TestSuite.Messaging.Debugger
{
	public class GetPossibleBreakpointsResponse : ProtocolObject
	{
		public Location[] Locations { get; set; }
	}
}
