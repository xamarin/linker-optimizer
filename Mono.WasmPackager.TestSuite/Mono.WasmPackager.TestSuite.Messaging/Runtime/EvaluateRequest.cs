namespace Mono.WasmPackager.TestSuite.Messaging.Runtime
{
	public class EvaluateRequest : ProtocolRequest<EvaluateResponse>
	{
		public override string Command => "Runtime.evaluate";

		public string Expression { get; set; }

		// Properties below are optional.
		public string ObjectGroup { get; set; }
		public bool? IncludeCommandLineAPI { get; set; }
		public bool? Silent { get; set; }
		public int? ExceptionContextId { get; set; }
		public bool? ReturnByValue { get; set; }
		public bool? GeneratePreview { get; set; }
		public bool? UserGesture { get; set; }
		public bool? AwaitPromise { get; set; }
	}
}
