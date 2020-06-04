namespace Mono.WasmPackager.TestSuite.Messaging.Runtime
{
	public class PrivatePropertyDescriptor : ProtocolObject
	{
		public string Name { get; set; }
		// Properties below are optional.
		public RemoteObject Value { get; set; }
		public RemoteObject Get { get; set; }
		public RemoteObject Set { get; set; }
	}
}
