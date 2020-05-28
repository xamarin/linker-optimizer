using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Martin.LinkerTest
{
	class TestConditionals
	{
		public static void Main ()
		{
			if (Property)
				Foo.Hello ();
		}

		internal static bool Property => MonoLinkerSupport.IsFeatureSupported (MonoLinkerFeature.Martin);
	}

	class Foo
	{
		public static void Hello ()
		{
			TestHelpers.AssertRemoved ();
		}
	}
}
