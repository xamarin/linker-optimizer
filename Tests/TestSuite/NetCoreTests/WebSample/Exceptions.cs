using System;

namespace NetCoreTests.WebSample
{
	public static class Exceptions
	{
		public static void Throw ()
		{
			// @@BREAKPOINT: Throw
			throw new MyException ("Throwing here.", 99999);
		}

		public static void Caught ()
		{
			// @@BEGIN-FUNCTION: Caught
			try {
				// @@LINE: CallingThrow
				Throw ();
			} catch (MyException ex) {
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
