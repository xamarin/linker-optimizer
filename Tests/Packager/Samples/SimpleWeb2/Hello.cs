using System;

namespace SimpleWeb2
{
	public class Hello
	{
		public static void Main ()
		{
			Console.WriteLine ("Hello World!");
		}

		public static void ButtonClicked ()
		{ // @@BEGIN-SCOPE
			// @@LINE: ButtonClicked
			throw new InvalidTimeZoneException ("I LIVE ON THE MOON!");
		} // @@END-SCOPE

		public static void TestMessage ()
		{
			/* @@BREAKPOINT: TestMessage */ // @@LINE: TestMessage2
			Console.WriteLine ($"HELLO WORLD!");
		}
	}
}
