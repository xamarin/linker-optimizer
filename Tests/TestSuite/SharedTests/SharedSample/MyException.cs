using System;

namespace SharedTests.SharedSample
{
	public class MyException : Exception
	{
		public readonly int Field;

		public int Property => Field;

		public MyException (string message, int value)
			: base (message)
		{
			Field = value;
		}

		public override string ToString ()
		{
			return $"[{GetType ().Name}: {Field}]";
		}
	}
}
