namespace Mono.WasmPackager.TestSuite
{
	public class BreakpointInfo
	{
		public SourceLocation Location {
			get;
		}

		public string Url {
			get;
		}

		public string ScriptId {
			get;
		}

		public string Id {
			get;
		}

		public BreakpointInfo (SourceLocation location, string url, string scriptId, string id)
		{
			Location = location;
			Url = url;
			ScriptId = scriptId;
			Id = id;
		}

		public override string ToString () => $"[{GetType ().Name} {Location} {Id}]";
	}
}
