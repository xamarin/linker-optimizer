using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace Martin.LinkerTest
{
	static class TestWeakInstances
	{
		public static void Main ()
		{
			Run (typeof (TestWeakInstances));
		}

		static void Run (Type type)
		{
			if ((object)type == null)
				throw new ArgumentNullException ("type");
			if (MonoLinkerSupport.IsWeakInstanceOf<TypeBuilder> (type))
				throw new AssertionException ("Conditional should return false.");

			Console.Error.WriteLine ("DONE");
		}
	}
}
