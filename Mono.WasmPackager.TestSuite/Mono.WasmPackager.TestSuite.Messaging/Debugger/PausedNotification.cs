namespace Mono.WasmPackager.TestSuite.Messaging.Debugger
{
	public class PausedNotification
	{
		public CallFrame[] CallFrames { get; set; }
		public StoppedReason Reason { get; set; }
		public string[] HitBreakpoints { get; set; }
	}
}
