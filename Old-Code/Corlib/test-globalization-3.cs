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

			var parsed = DateTime.Parse (now.ToString ());
			if (now.ToString () != parsed.ToString ())
				throw new AssertionException ();

			var dtfi = new DateTimeFormatInfo ();
			Console.WriteLine ($"DTFI: |{dtfi.DateSeparator}|{dtfi.TimeSeparator}| - {dtfi.DateSeparator == dtfi.TimeSeparator}");

			foreach (var pattern in dtfi.GetAllDateTimePatterns ('y'))
				Console.WriteLine ($"DTFI PATTERN: |{pattern}|");

			Console.WriteLine (dtfi.IsReadOnly);
		}
	}
}
