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

		public const string MyErrorMessage = "Error: MY ERROR";
		public const string StepOverFirstLine = "STEP-OVER-FIRST-LINE";
		public const string StepOverSecondLine = "STEP-OVER-SECOND-LINE";

		public const string VariablesClassName = "SharedTests.SharedSample.Variables";
		public const string MyErrorClassName = "SharedTests.SharedSample.MyError";
		public const string MyInstanceClassName = "SharedTests.SharedSample.Foo";

		public static class Selectors
		{
			public const string Output = "#output";
			public const string Message = "#message";
			public const string StepOver = "#stepOver";
			public const string ExceptionVariable = "#exceptionVariable";
			public const string ThrowExceptionVariable = "#throwExceptionVariable";
			public const string JsVariables = "#jsVariables";
			public const string JsException = "#jsException";
		}
	}
}