using Mono.WasmPackager.TestSuite;

namespace SimpleTest
{
	public static class TestConstants
	{
		// Keep in sync with the javascript side
		public const string MessageText = "MESSAGE BUTTON CLICKED";
		public const string MessageText2 = "MESSAGE BUTTON CLICKED - BACK FROM MANAGED";
		public const string CaughtExceptionText = "CAUGHT EXCEPTION BUTTON CLICKED";
		public const string CaughtExceptionText2 = "CAUGHT EXCEPTION BUTTON CLICKED - BACK FROM MANAGED";
		public const string TextReady = "READY";
		public const string TextMessage = "MESSAGE";
		public const string ThrowMessage = "THROW";

		public const int DefaultTimeout = 15;

		public const string HelloFile = "Hello.cs";
		public const string ExceptionFile = "Exceptions.cs";
		public const string MessageMethod = "Message";

		public static SourceLocation MessageBreakpoint = new SourceLocation (HelloFile, MessageMethod, 14, 2, 12, 14);
		public static SourceLocation ThrowMethod = new SourceLocation (ExceptionFile, "Throw", 8, 3, 7, 8);
		public static SourceLocation ThrownLocation = new SourceLocation (ExceptionFile, "Caught", 14, 4, 12, 18);
	}
}
