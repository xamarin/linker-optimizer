using System;

namespace SimpleTest.WebSample
{
	public class Hello
	{
		public static void Main ()
		{
			Console.WriteLine ("Hello World!");
		}

		public static void Message ()
		{
			Console.WriteLine ("Write Message");
		}

		public static void Throw ()
		{
			throw new InvalidOperationException ("Throwing here.");
		}
	}
}
