namespace Mono.WasmPackager.TestSuite.Messaging.Debugger
{
	using Runtime;

	public class Scope
	{
		public ScopeType Type { get; set; }
		public RemoteObject Object { get; set; }
		public string Name { get; set; }
		public Location StartLocation { get; set; }
		public Location EndLocation { get; set; }
	}
}
