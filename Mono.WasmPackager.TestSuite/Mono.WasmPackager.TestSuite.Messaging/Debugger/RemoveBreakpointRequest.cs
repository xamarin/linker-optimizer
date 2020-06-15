namespace Mono.WasmPackager.TestSuite.Messaging.Debugger
{
	public class RemoveBreakpointRequest : ProtocolRequest<RemoveBreakpointResponse>
	{
		public override string Command => "Debugger.removeBreakpoint";

		public string BreakpointId { get; set; }
	}
}
