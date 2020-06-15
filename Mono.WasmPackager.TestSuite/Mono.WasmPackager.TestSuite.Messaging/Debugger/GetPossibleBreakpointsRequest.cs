namespace Mono.WasmPackager.TestSuite.Messaging.Debugger
{
	public class GetPossibleBreakpointsRequest : ProtocolRequest<GetPossibleBreakpointsResponse>
	{
		public override string Command => "Debugger.getPossibleBreakpoints";

		public Location Start { get; set; }
		public Location End { get; set; }
		public bool RestrictToFunction { get; set; }
	}
}
