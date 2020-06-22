using System;

namespace SharedTests.SharedSample
{
	public class Foo
	{
		public readonly int Number = 8888;

		public Foo ()
		{
			Console.WriteLine ($"FOO CTOR");
		}

		public void Hello()
		{
			Console.WriteLine ($"FOO HELLO");
		}

		public void ThrowError()
		{
			var myError = new MyError ();
			Console.WriteLine ($"THROWING HERE"); // @BREAKPOINT ManagedFooThrowError
			throw myError;
		}

		public string ErrorProperty {
			get {
				var myError = new MyError ();
				Console.WriteLine ($"THROWING HERE"); // @BREAKPOINT ManagedFooErrorProperty
				throw myError;
			}
		}
	}
}
