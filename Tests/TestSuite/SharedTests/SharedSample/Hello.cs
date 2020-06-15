using System;

namespace SharedTests.SharedSample
{
	public class Hello
	{
		public static void Main ()
		{
			Console.WriteLine ("Hello World!");
		}

		public static void Message ()
		{ // @@BEGIN-SCOPE
			/* @@BREAKPOINT: Message */ Console.WriteLine ("Write Message");
		} // @@END-SCOPE

		public static void Throw ()
		{
			throw new InvalidOperationException (TestConstants.MyExceptionMessage);
		}

		// @@BEGIN-FUNCTION: StepOver
		public static void StepOver ()
		{ // @@BEGIN-SCOPE
			Console.WriteLine (TestConstants.StepOverFirstLine);  // @@BREAKPOINT: StepOverFirstLine
			Console.WriteLine (TestConstants.StepOverSecondLine); // @@LINE: StepOverSecondLine
		} // @@END-SCOPE
		// @@END-FUNCTION
	}
}
