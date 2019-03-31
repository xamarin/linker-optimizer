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
			bool supported;
			try {
				supported = MonoLinkerSupport.IsFeatureSupported (MonoLinkerFeature.Martin);
				if (supported)
					throw new AssertionException ("Feature `MonoLinkerFeature.Martin` should not be enabled.");
			} catch (Exception ex) {
				Console.Error.WriteLine ($"ERROR: {ex.Message}");
				throw;
			}

			Console.WriteLine ($"DONE: {supported}");
		}
	}
}
