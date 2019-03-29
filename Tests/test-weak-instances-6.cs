using System;
using System.Runtime.CompilerServices;

namespace Martin.LinkerTest
{
	static class TestWeakInstances
	{
		public static void Main ()
		{
			TestAvailable1 ();
			TestAvailable2 ();
		}

		public static void TestAvailable1 ()
		{
			var supported = MonoLinkerSupport.IsTypeAvailable<Foo> ();
			Console.Error.WriteLine ($"SUPPORTED: {supported}");
			if (supported)
				throw new AssertionException ("Conditional should have returned false.");
		}

		public static void TestAvailable2 ()
		{
			var supported = MonoLinkerSupport.IsTypeAvailable ("Martin.LinkerTest.Foo");
			Console.Error.WriteLine ($"SUPPORTED: {supported}");
			if (supported)
				throw new AssertionException ("Foo should not be available");

			if (MonoLinkerSupport.IsTypeAvailable ("Martin.LinkerTest.Undefined"))
				throw new InvalidOperationException ("Undefined type!");
		}
	}

	class Foo
	{
		public void Hello ()
		{
			Console.WriteLine ("Hello World!");
		}
	}
}
