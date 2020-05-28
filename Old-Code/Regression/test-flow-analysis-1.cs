using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace Martin.LinkerTest
{
	static class TestFlowAnalysis
	{
		public static void Main ()
		{
			new Foo ();
			bar = new Bar ();
			SerializeExceptionData (null);
		}

		static Bar bar;

		internal static byte[] SerializeExceptionData (Exception ex)
		{
			byte[] result = null;
			try {
				/* empty - we're only interested in the protected block */
			} finally {
				MemoryStream ms = new MemoryStream ();
				lock (bar) {
					bar.Hello (ms, ex);
				}
				result = ms.ToArray ();
			}
			return result;
		}
	}

	class Foo
	{
		~Foo ()
		{
			Console.WriteLine ("Finalizer!");
		}
	}

	class Bar
	{
		public void Hello (MemoryStream stream, Exception ex)
		{ }
	}
}
