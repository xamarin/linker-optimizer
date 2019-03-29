using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace Martin.LinkerTest
{
	static class TestFlowAnalysis
	{
		public static void Main ()
		{
			TryCatchMethod ();
			int argument = 0;
			var value = TestWithFinally (ref argument);
			TestHelpers.AssertEqual (1, value, "TestWithFinally");
			TestHelpers.AssertEqual (2, argument, "TestWithFinally");
		}

		public static bool TryCatchMethod ()
		{
			try
			{
				throw new NotSupportedException ();
			}
			catch
			{
				return false;
			}
		}

		public static int TestWithFinally (ref int argument)
		{
			int value = 0;
			try {
				Console.WriteLine ("Hello!");
				argument++;
				value++;
				return value;
			} finally {
				Console.WriteLine ("Finally!");
				argument++;
				// This won't modify the return value.
				value++;
			}

			throw TestHelpers.AssertRemoved ();
		}
	}
}
