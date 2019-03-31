using System;
using System.Runtime.CompilerServices;

namespace Martin.LinkerTest
{
	static class TestWeakInstances
	{
		public static void Main ()
		{
			Foo.Hello ();

			RunWeakInstance1 ();
			RunWeakInstance2 ();
			RunWeakInstance3 ();
			RunWeakInstance4 ();

			var supported = RunWeakInstance5 ();
			TestHelpers.Assert (!supported);

			RunWeakInstance6 (out supported);
			TestHelpers.Assert (!supported);

			RunWeakInstance7 (ref supported);
			TestHelpers.Assert (!supported);

			RunWeakInstance8 ();

			supported = RunWeakInstance9 (null);
			TestHelpers.Assert (!supported);

			RunWeakInstance10 ();
			RunWeakInstance11 ();
			RunWeakInstance12 ();
			RunWeakInstance13 ();
		}

		public static void RunWeakInstance1 ()
		{
			TryCatchMethod ();
			if (MonoLinkerSupport.IsWeakInstanceOf<Foo> (null)) {
				TestHelpers.Debug ("Conditional should return false.");
				TestHelpers.AssertFail ("Conditional should return false.");
			}

			Console.Error.WriteLine ("DONE");
		}

		public static void RunWeakInstance2 ()
		{
			if (MonoLinkerSupport.IsWeakInstanceOf<Foo> (null))
				throw new AssertionException ("Conditional should return false.");

			Console.Error.WriteLine ("DONE");
		}

		public static void RunWeakInstance3 ()
		{
			Console.Error.WriteLine ("HELLO");
			if (MonoLinkerSupport.IsWeakInstanceOf<Foo> (null))
				throw new AssertionException ("Conditional should return false.");

			Console.Error.WriteLine ("DONE");
		}

		public static void RunWeakInstance4 ()
		{
			var supported = MonoLinkerSupport.IsWeakInstanceOf<Foo> (null);
			Console.Error.WriteLine ($"SUPPORTED: {supported}");
			TestHelpers.Assert (!supported);
		}

		public static bool RunWeakInstance5 ()
		{
			return MonoLinkerSupport.IsWeakInstanceOf<Foo> (null);
		}

		public static void RunWeakInstance6 (out bool supported)
		{
			supported = MonoLinkerSupport.IsWeakInstanceOf<Foo> (null);
		}

		public static void RunWeakInstance7 (ref bool supported)
		{
			supported = MonoLinkerSupport.IsWeakInstanceOf<Foo> (null);
		}

		public static void RunWeakInstance8 ()
		{
			var instance = new InstanceTest ();
			instance.Run ();
			Console.WriteLine (instance.Supported);
			TestHelpers.Assert (!instance.Supported);
		}

		public static bool RunWeakInstance9 (object instance)
		{
			return MonoLinkerSupport.IsWeakInstanceOf<Foo> (instance);
		}

		public static bool RunWeakInstance10 ()
		{
			var instance = new InstanceTest ();
			Console.WriteLine (instance);
			if (TryCatchMethod ())
				throw new AssertionException ();
			return MonoLinkerSupport.IsWeakInstanceOf<Foo> (instance.Field);
		}

		public static void RunWeakInstance11 ()
		{
			Console.WriteLine (MonoLinkerSupport.IsWeakInstanceOf<Foo> (null));
			Console.WriteLine ();
		}

		public static void RunWeakInstance12 ()
		{
			var instance = new InstanceTest ();
			Console.WriteLine (MonoLinkerSupport.IsWeakInstanceOf<Foo> (instance.Instance));
			Console.WriteLine ();
		}

		public static void RunWeakInstance13 ()
		{
			var instance = new InstanceTest ();
			if (MonoLinkerSupport.IsWeakInstanceOf<Foo> (instance.Instance))
				throw new AssertionException ("Conditional should return false.");

			Console.Error.WriteLine ("DONE");
		}

		public static bool TryCatchMethod ()
		{
			try
			{
				throw new InvalidOperationException ();
			}
			catch
			{
				return false;
			}
		}

		class InstanceTest
		{
			bool supported;

			public bool Supported => supported;

			public object Field = null;

			public object Instance => this;

			public void Run ()
			{
				supported = MonoLinkerSupport.IsWeakInstanceOf<Foo> (null);
			}
		}
	}

	public class Foo
	{
		public static void Hello ()
		{
			Console.WriteLine ("World");
		}
	}

	public class Bar
	{
		public static void Hello ()
		{
			Console.WriteLine ("I am not Foo!");
		}
	}
}
