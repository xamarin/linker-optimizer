using System;

namespace SimpleTest.WebSample
{
	public static class Exceptions
	{
		public static void Throw ()
		{
			throw new MyException ("Throwing here.", 99999);
		}

		public static void Caught ()
		{
			try {
				Throw ();
			} catch (MyException ex) {
				Console.WriteLine ($"Caught Exception: {ex}");
			}
		}

		public static void Unhandled ()
		{
			Throw ();
		}
	}
}
