namespace Mono.WasmPackager.TestSuite.Messaging.Runtime
{
	public class GetPropertiesRequest : ProtocolRequest<GetPropertiesResponse>
	{
		public override string Command => "Runtime.getProperties";

		public string ObjectId { get; set; }
		// Properties below are optional.
		public bool OwnProperties { get; set; }
		public bool AccessorPropertiesOnly { get; set; }
		public bool GeneratePreview { get; set; }
	}
}
