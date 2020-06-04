namespace Mono.WasmPackager.TestSuite.Messaging.Runtime
{
	public class PropertyDescriptor : ProtocolObject
	{
		public string Name { get; set; }
		public bool Configurable { get; set; }
		public bool Enumerable { get; set; }
		// Properties below are optional.
		public RemoteObject Value { get; set; }
		public bool Writable { get; set; }
		public RemoteObject Get { get; set; }
		public RemoteObject Set { get; set; }
		public bool WasThrown { get; set; }
		public bool IsOwn { get; set; }
		public RemoteObject Symbol { get; set; }
	}
}
