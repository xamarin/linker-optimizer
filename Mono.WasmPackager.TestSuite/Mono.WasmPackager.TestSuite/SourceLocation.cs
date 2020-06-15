namespace Mono.WasmPackager.TestSuite
{
	public class SourceLocation
	{
		public string FullPath {
			get;
		}

		public string File {
			get;
		}

		public string SourcePath {
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

		public bool IsNative {
			get;
		}

		public SourceLocation (string fullPath, string file, string source, bool native, string name, int line, int? column = null, int? scopeStart = null, int? scopeEnd = null)
		{
			FullPath = fullPath;
			File = file;
			SourcePath = source;
			IsNative = native;
			FunctionName = name;
			Line = line;
			Column = column;
			ScopeStart = scopeStart;
			ScopeEnd = scopeEnd;
			IsNative = native;
		}

		public override string ToString () => $"[{GetType ().Name} {File} {FunctionName} {Line}]";
	}
}
