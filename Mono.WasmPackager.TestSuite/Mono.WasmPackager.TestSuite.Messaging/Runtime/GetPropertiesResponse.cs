namespace Mono.WasmPackager.TestSuite.Messaging.Runtime
{
	public class GetPropertiesResponse : ProtocolObject
	{
		public string ObjectId { get; set; }
		// Properties below are optional.
		public PropertyDescriptor [] Result { get; set; }
		public InternalPropertyDescriptor [] InternalProperties { get; set; }
		public PrivatePropertyDescriptor [] PrivateProperties { get; set; }
		public ExceptionDetails ExceptionDetails { get; set; }
	}
}
