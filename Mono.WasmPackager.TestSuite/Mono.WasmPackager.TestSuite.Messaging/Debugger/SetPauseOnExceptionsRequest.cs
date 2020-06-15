namespace Mono.WasmPackager.TestSuite.Messaging.Debugger
{
	public class SetPauseOnExceptionsRequest : ProtocolRequest<SetPauseOnExceptionsResponse>
	{
		public override string Command => "Debugger.setPauseOnExceptions";

		public PauseOnExceptionMode State {
			get; set;
		}
	}
}
