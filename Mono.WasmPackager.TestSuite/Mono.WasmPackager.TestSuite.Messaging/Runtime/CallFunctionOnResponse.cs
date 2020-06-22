namespace Mono.WasmPackager.TestSuite.Messaging.Runtime
{
	public class CallFunctionOnResponse : ProtocolResponse
	{
		public RemoteObject Result { get; set; }
		// Properties below are optional.
		public ExceptionDetails ExceptionDetails { get; set; }
	}
}
