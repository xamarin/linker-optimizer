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
		{
			throw new InvalidTimeZoneException ("I LIVE ON THE MOON!");
		}
	}
}
