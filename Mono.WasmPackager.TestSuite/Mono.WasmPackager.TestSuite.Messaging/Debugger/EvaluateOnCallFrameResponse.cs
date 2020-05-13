namespace Mono.WasmPackager.TestSuite.Messaging.Debugger
{
	using Runtime;

	public class EvaluateOnCallFrameResponse
	{
		public RemoteObject Result { get; set; }
		// Optional
		public ExceptionDetails ExceptionDetails { get; set; }
	}
}
