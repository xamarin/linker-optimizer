namespace Mono.WasmPackager.TestSuite.Messaging.Runtime
{
	public class RemoteObject : ProtocolObject
	{
		public RemoteObjectType Type { get; set; }
		public RemoteObjectSubType SubType { get; set; }
		public object Value { get; set; }
		public string ClassName { get; set; }
		public string Description { get; set; }
		public string ObjectId { get; set; }
	}
}
