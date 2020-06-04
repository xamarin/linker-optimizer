namespace Mono.WasmPackager.TestSuite.Messaging.Runtime
{
	public class ExceptionDetails : ProtocolObject
	{
		public int ExceptionId { get; set; }
		public string Text { get; set; }
		public int LineNumber { get; set; }
		public int ColumnNumber { get; set; }
		// All properties below are optional
		public string ScriptId { get; set; }
		public string Url { get; set; }
		// public StackTrace StackTrace { get; set; }
		public RemoteObject Exception { get; set; }
		// public ExceptionContextId ExceptionContextId { get; set; }
	}
}
