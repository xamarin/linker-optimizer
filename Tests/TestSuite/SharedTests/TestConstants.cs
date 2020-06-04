using Mono.WasmPackager.TestSuite;

namespace SharedTests
{
	public static class TestConstants
	{
		// Keep in sync with the javascript side
		public const string MessageText = "MESSAGE BUTTON CLICKED";
		public const string MessageText2 = "MESSAGE BUTTON CLICKED - BACK FROM MANAGED";
		public const string CaughtExceptionText = "CAUGHT EXCEPTION BUTTON CLICKED";
		public const string CaughtExceptionText2 = "CAUGHT EXCEPTION BUTTON CLICKED - BACK FROM MANAGED";
		public const string VariablesText = "VARIABLES BUTTON CLICKED";
		public const string VariablesText2 = "VARIABLES BUTTON CLICKED - BACK FROM MANAGED";
		public const string TextReady = "READY";
		public const string TextMessage = "MESSAGE";
		public const string ThrowMessage = "THROW";

		public const int DefaultTimeout = 15;

		public const string MyExceptionMessage = "Throwing here.";

		public const string MyExceptionClassName = "SharedTests.SharedSample.MyException";
	}
}
