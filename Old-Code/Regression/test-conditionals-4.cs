using System;
using System.Runtime.CompilerServices;

namespace Martin.LinkerTest
{
	class TestConditionals
	{
		public static void Main ()
		{
			var supported = MonoLinkerSupport.IsFeatureSupported (MonoLinkerFeature.Martin) && ReturnTrue ();
			Console.Error.WriteLine ($"SUPPORTED: {supported}");

			if (supported) {
				Foo.Hello ();
			}
		}

		static bool ReturnTrue ()
		{
			return false;
		}
	}

	class Foo
	{
		public static void Hello ()
		{
			throw TestHelpers.AssertRemoved ();
		}
	}

}
