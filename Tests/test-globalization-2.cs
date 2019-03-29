using System;
using System.Text;
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

			var ascii = Encoding.ASCII.GetBytes ("Hello");
			var utf8 = Encoding.UTF8.GetBytes ("Hello");
			var utf32 = Encoding.UTF32.GetBytes ("Hello");
			Console.WriteLine ($"ENCODED: {ascii.Length} {utf8.Length} {utf32.Length}");

			try {
				Encoding.GetEncoding ("ascii");
				throw new AssertionException ("Encoding.GetEncoding() should throw!");
			} catch (PlatformNotSupportedException) { }

			Test ();
		}

		static void Test ()
		{
			if (MonoLinkerSupport.IsTypeAvailable ("System.Globalization.EncodingTable"))
				throw new AssertionException ("System.Globalization.EncodingTable");
		}
	}
}
