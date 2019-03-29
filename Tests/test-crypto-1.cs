using System;
using System.Text;
using System.Security.Cryptography;
using System.Runtime.CompilerServices;

namespace Martin.LinkerTest
{
	static class TestCrypto
	{
		public static void Main ()
		{
			TestHelpers.AssertNotNull (HashAlgorithm.Create ("SHA1"), "SHA1");
			TestHelpers.AssertNotNull (HashAlgorithm.Create ("MD5"), "MD5");

			TestHelpers.AssertNotNull (CryptoConfig.CreateFromName ("SHA1"), "`CryptoConfig.CreateFromName (\"SHA1\")`");

			TestHelpers.AssertNotNull (Aes.Create (), "Aes.Create()");

		}
	}
}
