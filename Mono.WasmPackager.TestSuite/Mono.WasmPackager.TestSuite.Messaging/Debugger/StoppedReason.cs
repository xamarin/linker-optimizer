namespace Mono.WasmPackager.TestSuite.Messaging.Debugger
{
	public enum StoppedReason
	{
		Ambiguous,
		Assert,
		DebugCommand,
		DOM,
		EventListener,
		Exception,
		Instrumentation,
		OOM,
		Other,
		PromiseRejection,
		XHR
	}
}
