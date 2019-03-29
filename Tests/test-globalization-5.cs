using System;
using System.Runtime.CompilerServices;

namespace Martin.LinkerTest
{
	static class TestGlobalization
	{
		public static void Main ()
		{
			var result = String.Compare ("A", "a");
			Console.WriteLine (result);

			Test ();
		}

		static void Test ()
		{
			if (MonoLinkerSupport.IsTypeAvailable ("Mono.Globalization.Unicode.SimpleCollator"))
				throw new AssertionException ("Mono.Globalization.Unicode.SimpleCollator");
		}
	}
}
