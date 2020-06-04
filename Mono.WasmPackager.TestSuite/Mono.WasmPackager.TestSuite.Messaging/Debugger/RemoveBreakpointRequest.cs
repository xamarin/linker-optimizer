namespace Mono.WasmPackager.TestSuite.Messaging.Debugger
{
	public class RemoveBreakpointRequest : ProtocolObject
	{
		public string BreakpointId { get; set; }
	}
}
