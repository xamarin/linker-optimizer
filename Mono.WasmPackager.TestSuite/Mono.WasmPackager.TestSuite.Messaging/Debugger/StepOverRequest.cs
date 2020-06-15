namespace Mono.WasmPackager.TestSuite.Messaging.Debugger
{
	public class StepOverRequest : ProtocolRequest<StepOverResponse>
	{
		public override string Command => "Debugger.stepOver";
	}
}
