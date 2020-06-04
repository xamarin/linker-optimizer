namespace Mono.WasmPackager.TestSuite.Messaging.Debugger
{
	using Runtime;

	public class CallFrame : ProtocolObject
	{
		public string CallFrameId { get; set; }
		public string FunctionName { get; set; }
		public Location FunctionLocation { get; set; }
		public Location Location { get; set; }
		public string Url { get; set; }
		public Scope[] ScopeChain { get; set; }
		public RemoteObject This { get; set; }
		public RemoteObject ReturnValue { get; set; }
	}
}
