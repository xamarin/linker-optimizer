using System;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Martin.LinkerTest
{
	class TestFlowAnalysis
	{
		public static void Main ()
		{
			Console.WriteLine ("Hello!");

			var instance = new TestFlowAnalysis ();
			instance.ToLocalTime (false);

			var now = DateTime.Now;
			Console.WriteLine (DateTime.Now);

			var local = now.ToLocalTime();
			Console.WriteLine (local);
		}

		DateTimeKind Kind => DateTimeKind.Local;

		long Ticks => 0;
		long MinTicks => 0;
		long MaxTicks => 0;

		public static bool HasGlobalization => MonoLinkerSupport.IsFeatureSupported (MonoLinkerFeature.Unknown);

		TestFlowAnalysis ToLocalTime (bool throwOnOverflow)
		{
			if (Kind == DateTimeKind.Local)
				return this;
				
			Boolean isDaylightSavings = false;
			Boolean isAmbiguousLocalDst = false;
			Int64 offset;
			if (HasGlobalization)
			{
				offset = GetUtcOffsetFromUtc (this, out isDaylightSavings, out isAmbiguousLocalDst).Ticks;
			}
			else
			{
				offset = 0;
			}
			long tick = Ticks + offset;
			if (tick > MaxTicks) {
				if (throwOnOverflow)
					throw new ArgumentException ();
				else
					return new TestFlowAnalysis (MaxTicks, DateTimeKind.Local);
			}
			if (tick < MinTicks) {
				if (throwOnOverflow)
					throw new ArgumentException ();
				else
					return new TestFlowAnalysis (MinTicks, DateTimeKind.Local);
			}
			return new TestFlowAnalysis (tick, DateTimeKind.Local, isAmbiguousLocalDst);
		}

		TestFlowAnalysis ()
		{ }

		TestFlowAnalysis (long tick, DateTimeKind kind)
		{ }

		TestFlowAnalysis (long tick, DateTimeKind kind, bool isAmbiguousLocalDst)
		{ }

		static TestFlowAnalysis GetUtcOffsetFromUtc (TestFlowAnalysis instance, out bool isDaylightSavings, out bool isAmbiguousLocalDst)
		{
			isDaylightSavings = false;
			isAmbiguousLocalDst = false;
			return instance;
		}
	}
}
