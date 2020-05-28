using System;
using System.Runtime.CompilerServices;

namespace Martin.LinkerTest
{
	static class TestWeakInstances
	{
		public static void Main ()
		{
			RunWeakInstance (null);
			RunWeakInstance (new Bar ());
		}

		public static void RunWeakInstance (object instance)
		{
			if (MonoLinkerSupport.AsWeakInstanceOf<Foo> (instance, out var foo)) {
				TestHelpers.Debug ("Conditional should have been removed.");
				foo.Hello ();
				TestHelpers.AssertRemoved ();
			}

			Console.Error.WriteLine ("DONE");
		}
	}

	public class Foo
	{
		public void Hello ()
		{
			Console.WriteLine ("World");
		}
	}

	public class Bar
	{
		public void Hello ()
		{
			Console.WriteLine ("I am not Foo!");
		}
	}
}
