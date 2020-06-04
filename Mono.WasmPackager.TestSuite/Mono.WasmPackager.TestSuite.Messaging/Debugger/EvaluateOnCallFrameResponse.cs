namespace Mono.WasmPackager.TestSuite.Messaging.Debugger
{
	using Runtime;

	public class EvaluateOnCallFrameResponse : ProtocolObject
	{
		public RemoteObject Result { get; set; }
		// Optional
		public ExceptionDetails ExceptionDetails { get; set; }
	}
}
