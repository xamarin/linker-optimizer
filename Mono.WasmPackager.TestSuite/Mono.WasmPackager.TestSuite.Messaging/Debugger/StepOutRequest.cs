namespace Mono.WasmPackager.TestSuite.Messaging.Debugger
{
	public class StepOutRequest : ProtocolRequest<StepOutResponse>
	{
		public override string Command => "Debugger.stepOut";
	}
}
