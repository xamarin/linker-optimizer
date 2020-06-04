namespace Mono.WasmPackager.TestSuite.Messaging.Runtime
{
	public class InternalPropertyDescriptor : ProtocolObject
	{
		public string Name { get; set; }
		// Properties below are optional.
		public RemoteObject Value { get; set; }
	}
}
