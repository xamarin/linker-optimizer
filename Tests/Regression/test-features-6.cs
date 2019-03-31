using System;
using System.Runtime.CompilerServices;

namespace Martin.LinkerTest
{
	static class TestFeatures
	{
		public static void Main ()
		{
			RunFeature ();
		}

		public static void RunFeature ()
		{
			if (!MonoLinkerSupport.IsFeatureSupported (MonoLinkerFeature.Martin))
				throw new InvalidOperationException ("Expected `MonoLinkerFeature.Martin` to be enabled.");
			if (MonoLinkerSupport.IsFeatureSupported (MonoLinkerFeature.Globalization))
				throw new InvalidOperationException ("Expected `MonoLinkerFeature.Globalization` to be disabled.");
		}
	}
}
