using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace Martin.LinkerTest
{
	static class TestFlowAnalysis
	{
		static object locker = new object ();
		static Foo instance;

		public static void Main ()
		{
			var array = new[] { "Hello", "Hello World", "World" };
			var space = ArrayElementsHaveSpace (array);
			Console.Error.WriteLine (space);
			TryCatchMethod ();
			TestFinally ();
		}

		static bool ArrayElementsHaveSpace (string[] array)
		{
			for (int i = 0; i < array.Length; i++) {
				// it is faster to check for space character manually instead of calling IndexOf
				// so we don't have to go to native code side.
				for (int j = 0; j < array[i].Length; j++) {
					if (Char.IsWhiteSpace(array[i][j])){
						return true;
					}
				}
			}

			return false;
		}

		public static bool TryCatchMethod ()
		{
			try
			{
				throw new NotSupportedException ();
			}
			catch
			{
				return false;
			}
		}

		public static Foo TestFinally ()
		{
			if (instance != null)
				return instance;

			lock (locker) {
				Console.WriteLine ($"IN LOCK");
				instance = new Foo ();
			}

			instance.Hello ();
			return instance;
		}
	}

	class Foo
	{
		public void Hello ()
		{
			Console.WriteLine ("World");
		}
	}
}
