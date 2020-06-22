namespace Mono.WasmPackager.TestSuite.Messaging.Runtime
{
	public class CallFunctionOnRequest : ProtocolRequest<CallFunctionOnResponse>
	{
		public override string Command => "Runtime.callFunctionOn";

		public string ObjectId { get; set; }
		public string FunctionDeclaration { get; set; }

		// Properties below are optional.
		public CallArgument [] Argu { get; set; }
		public bool? Silent { get; set; }
		public bool? ReturnByValue { get; set; }
	}
}
