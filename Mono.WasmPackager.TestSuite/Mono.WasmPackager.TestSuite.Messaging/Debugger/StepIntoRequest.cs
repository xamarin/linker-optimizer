namespace Mono.WasmPackager.TestSuite.Messaging.Debugger
{
	public class StepIntoRequest : ProtocolRequest<StepIntoResponse>
	{
		public override string Command => "Debugger.stepInto";

		public bool BreakOnAsyncClass { get; set; }
	}
}
