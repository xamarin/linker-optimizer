using System;
using System.Runtime.CompilerServices;

namespace Martin.LinkerTest
{
	static class TestWeakInstances
	{
		public static void Main ()
		{
			RunWeakInstance ();
		}

		public static void RunWeakInstance ()
		{
			if (MonoLinkerSupport.IsWeakInstanceOf<Foo> (null)) {
				Foo.Hello ();
				TestHelpers.Debug ("Conditional should be linked out.");
				throw TestHelpers.AssertRemoved ();
			}

			Console.Error.WriteLine ("DONE");
		}
	}

	public class Foo
	{
		public static void Hello ()
		{
			Console.WriteLine ("World");
		}
	}
}
