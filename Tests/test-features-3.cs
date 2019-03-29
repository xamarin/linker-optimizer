using System;
using System.Runtime.CompilerServices;

namespace Martin.LinkerTest
{
	static class TestFeatures
	{
		public static void Main ()
		{
			RunFeature (false);
			RunFeature (true);
		}

		public static void RunFeature (bool test)
		{
			if (test)
				return;
			if (MonoLinkerSupport.IsFeatureSupported (MonoLinkerFeature.Martin))
				throw new AssertionException ("Feature `MonoLinkerFeature.Martin` should be disabled.");
		}
	}
}
