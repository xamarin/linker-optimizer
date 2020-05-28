using System;
using System.Text;
using System.Runtime.CompilerServices;

namespace Martin.LinkerTest
{
	static class TestSecurity
	{
		public static void Main ()
		{
			Console.WriteLine ("Hello!");

			Test ();
		}

		static void Test ()
		{
			if (MonoLinkerSupport.IsTypeAvailable ("System.Security.Cryptography.X509Certificate"))
				throw new AssertionException ("System.Security.Cryptography.X509Certificate");
		}
	}
}
