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
			if (!Property2)
				Foo.Hello ();

			var instance = new TestConditionals ();
			var retval = instance.InstanceProperty;

			instance.Test ();
			var retval2 = instance.InstanceProperty;

			if (retval)
				throw new AssertionException ();
			if (retval2)
				throw new AssertionException ();
		}

		static bool value;

		internal static bool Property {
			get {
				return MonoLinkerSupport.IsFeatureSupported (MonoLinkerFeature.Martin) && ReturnFalse ();
			}
		}

		internal static bool Property2 {
			get {
				return !MonoLinkerSupport.IsFeatureSupported (MonoLinkerFeature.Martin) || value;
			}
		}

		internal bool InstanceProperty {
			get {
				return MonoLinkerSupport.IsFeatureSupported (MonoLinkerFeature.Martin) && ReturnFalse ();
			}
		}

		static bool ReturnFalse ()
		{
			return false;
		}

		void Test ()
		{
			Console.WriteLine ("Test");
		}
	}

	class Foo
	{
		public static void Hello ()
		{
			TestHelpers.AssertRemoved ();
		}
	}
}
