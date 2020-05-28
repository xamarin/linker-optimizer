using System;

namespace WorkingTests.WebSample
{
	public class MyObject
	{
		public readonly double PI;
		public readonly double PiDividedByE;
		public readonly double PiAtThePowerOfE;

		public readonly int SomeInteger = 12345678;
		public readonly int MaxInt = int.MaxValue;
		public readonly int MinInt = int.MinValue;

		public readonly long MaxLong = long.MaxValue;
		public readonly long MinLong = long.MinValue;

		public readonly ulong MaxULong = ulong.MaxValue;
		public readonly ulong MinULong = ulong.MinValue;

		public string PropertyThrows {
			get {
				throw new MyException ("Property Throws", 88888);
			}
		}

		public MyObject ()
		{
			PI = Math.PI;
			PiDividedByE = Math.PI / Math.E;
			PiAtThePowerOfE = Math.Pow (Math.PI, Math.E);
		}
	}
}
