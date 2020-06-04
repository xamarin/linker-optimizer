namespace Mono.WasmPackager.TestSuite.Messaging.Debugger
{
	public class Location : ProtocolObject
	{
		public string ScriptId { get; set; }
		public int LineNumber { get; set; }
		public int ColumnNumber { get; set; }
	}
}
