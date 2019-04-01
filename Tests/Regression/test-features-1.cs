using System;
using System.Runtime.CompilerServices;

namespace Martin.LinkerTest
{
	static class TestFeatures
	{
		public static void Main ()
		{
			RunFeature1 ();

			var supported = RunFeature2 ();
			TestHelpers.Assert (!supported);

			supported = RunFeature3 ();
			TestHelpers.Assert (!supported);
		}

		public static void RunFeature1 ()
		{
			if (MonoLinkerSupport.IsFeatureSupported (MonoLinkerFeature.Unknown))
				TestHelpers.AssertFail ("Feature `MonoLinkerFeature.Unknown` should be disabled.");
		}

		public static bool RunFeature2 ()
		{
			return MonoLinkerSupport.IsFeatureSupported (MonoLinkerFeature.Unknown);
		}

		public static bool RunFeature3 ()
		{
			return MonoLinkerSupport.IsFeatureSupported (MonoLinkerFeature.Martin);
		}
	}
}
