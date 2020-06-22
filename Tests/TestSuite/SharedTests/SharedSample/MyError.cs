using System;

namespace SharedTests.SharedSample
{
	public class MyError : Exception
	{
		public readonly string Hello;
		public readonly int Foo;

		public MyError ()
			: base ("MY ERROR")
		{
			Hello = "World";
			Foo = 999;
		}
	}
}
