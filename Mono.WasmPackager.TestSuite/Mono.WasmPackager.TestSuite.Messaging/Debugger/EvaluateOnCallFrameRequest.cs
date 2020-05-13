namespace Mono.WasmPackager.TestSuite.Messaging.Debugger
{
	public class EvaluateOnCallFrameRequest
	{
		public string CallFrameId { get; set; }
		public string Expression { get; set; }

		// Optional parameters below
		public string ObjectGroup { get; set; }
		public bool IncludeCommandLineAPI { get; set; }
		public bool Silent { get; set; }
		public bool ReturnByValue { get; set; }
		public bool GeneratePreview { get; set; }
		public bool ThrowOnSideEffect { get; set; }
	}
}
