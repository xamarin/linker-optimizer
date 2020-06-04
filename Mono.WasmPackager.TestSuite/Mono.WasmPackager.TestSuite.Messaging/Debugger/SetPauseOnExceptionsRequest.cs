namespace Mono.WasmPackager.TestSuite.Messaging.Debugger
{
	public class SetPauseOnExceptionsRequest : ProtocolObject
	{
		public PauseOnExceptionMode State {
			get; set;
		}
	}
}
