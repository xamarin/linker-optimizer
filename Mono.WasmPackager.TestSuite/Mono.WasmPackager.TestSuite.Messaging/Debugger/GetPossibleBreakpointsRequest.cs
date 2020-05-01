namespace Mono.WasmPackager.TestSuite.Messaging.Debugger
{
	public class GetPossibleBreakpointsRequest
	{
		public Location Start { get; set; }
		public Location End { get; set; }
		public bool RestrictToFunction { get; set; }
	}
}
