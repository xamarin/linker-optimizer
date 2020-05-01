namespace Mono.WasmPackager.TestSuite.Messaging.Runtime
{
	public class RemoteObject
	{
		public RemoveObjectType Type { get; set; }
		public string Subtype { get; set; }
		public object Value { get; set; }
		public string ClassName { get; set; }
		public string Description { get; set; }
	}
}
