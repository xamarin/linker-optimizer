using System;
using System.Runtime.CompilerServices;

namespace Martin.LinkerTest
{
	static class TestFeatures
	{
		public static void Main ()
		{
			RunFeature1 ();
			RunFeature2 ();
		}

		public static void RunFeature1 ()
		{
			if (!MonoLinkerSupport.IsFeatureSupported (MonoLinkerFeature.Martin))
				TestHelpers.AssertFail ("Feature `MonoLinkerFeature.Martin` should not be available.");
		}

		public static void RunFeature2 ()
		{
			if (MonoLinkerSupport.IsFeatureSupported (MonoLinkerFeature.Remoting))
				TestHelpers.AssertFail ("Remoting should be disabled.");
		}
	}
}
