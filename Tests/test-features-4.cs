using System;
using System.Runtime.CompilerServices;

namespace Martin.LinkerTest
{
	static class TestFeatures
	{
		public static void Main ()
		{
			RunFeature (MonoLinkerFeature.Martin);
		}

		public static void RunFeature (MonoLinkerFeature feature)
		{
			bool supported;
			switch (feature) {
			case MonoLinkerFeature.Martin:
				supported = MonoLinkerSupport.IsFeatureSupported (MonoLinkerFeature.Martin);
				break;
			case MonoLinkerFeature.Remoting:
				supported = MonoLinkerSupport.IsFeatureSupported (MonoLinkerFeature.Remoting);
				break;
			case MonoLinkerFeature.ReflectionEmit:
				Console.Error.WriteLine ("REFLECTION EMIT");
				supported = false;
				break;
			default:
				throw new AssertionException ();
			}

			Console.WriteLine ($"DONE: {supported}");
		}
	}
}
