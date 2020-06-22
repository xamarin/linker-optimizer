using System;

namespace SharedTests.SharedSample
{
	public static class Exceptions
	{
		public static void Throw ()
		{
			// @@BREAKPOINT: Throw
			throw new MyError ();
		}

		public static void Caught ()
		{
			// @@BEGIN-FUNCTION: Caught
			try {
				// @@LINE: CallingThrow
				Throw ();
			} catch (MyError ex) {
				// @@LINE: Caught
				Console.WriteLine ($"Caught Exception: {ex}");
			}
			// @@END-FUNCTION
		}

		public static void Unhandled ()
		{
			// @@LINE: Unhandled
			Throw ();
		}
	}
}
