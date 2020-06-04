namespace Mono.WasmPackager.TestSuite.Messaging.Debugger
{
	public class GetPossibleBreakpointsRequest : ProtocolObject
	{
		public Location Start { get; set; }
		public Location End { get; set; }
		public bool RestrictToFunction { get; set; }
	}
}
