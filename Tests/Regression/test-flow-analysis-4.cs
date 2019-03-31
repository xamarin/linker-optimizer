using System;
using System.Runtime.CompilerServices;

namespace Martin.LinkerTest
{
	class TestFlowAnalysis
	{
		static object locker = new object ();

		public static void Main ()
		{
			if (!MonoLinkerSupport.IsFeatureSupported (MonoLinkerFeature.Martin))
				return;

			lock (locker) {
				Hello ();
			}
		}

		[MethodImplAttribute (MethodImplOptions.NoInlining)]
		static void Hello ()
		{
			Console.WriteLine ("Hello!");
		}
	}
}
