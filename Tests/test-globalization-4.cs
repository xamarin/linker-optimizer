using System;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Martin.LinkerTest
{
	static class TestGlobalization
	{
		public static void Main ()
		{
			Console.WriteLine ("Hello!");

			var now = DateTime.Now;
			Console.WriteLine (DateTime.Now);

			var local = now.ToLocalTime ();
			Console.WriteLine (local);
		}
	}
}
