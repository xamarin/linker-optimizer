namespace Mono.WasmPackager.TestSuite.Messaging.Debugger
{
	public class ResumeRequest : ProtocolRequest<ResumeResponse>
	{
		public override string Command => "Debugger.resume";

		public bool TerimnateOnResume { get; set; }
	}
}
