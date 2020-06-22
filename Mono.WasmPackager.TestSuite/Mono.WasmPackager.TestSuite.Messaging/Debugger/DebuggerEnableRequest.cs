namespace Mono.WasmPackager.TestSuite.Messaging.Debugger
{
	public class DebuggerEnableRequest : ProtocolRequest<DebuggerEnableResponse>
	{
		public override string Command => "Debugger.enable";

		// Optional parameters below
		public int? MaxScriptCacheSize { get; set; }
	}
}
