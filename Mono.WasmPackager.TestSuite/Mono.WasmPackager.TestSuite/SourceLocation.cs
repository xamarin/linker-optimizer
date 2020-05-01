namespace Mono.WasmPackager.TestSuite
{
	public class SourceLocation
	{
		public string File {
			get;
		}

		public string FunctionName {
			get;
		}

		public int Line {
			get;
		}

		public int? Column {
			get;
		}

		public int? ScopeStart {
			get;
		}

		public int? ScopeEnd {
			get;
		}

		public SourceLocation (string file, string name, int line, int? column = null, int? scopeStart = null, int? scopeEnd = null)
		{
			File = file;
			FunctionName = name;
			Line = line;
			Column = column;
			ScopeStart = scopeStart;
			ScopeEnd = scopeEnd;
		}

		public override string ToString () => $"[{GetType ().Name} {File} {FunctionName} {Line}]";
	}
}
