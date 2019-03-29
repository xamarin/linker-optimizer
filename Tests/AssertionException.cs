using System;

namespace Martin.LinkerTest
{
	public class AssertionException : Exception
	{
		public AssertionException ()
		{ }

		public AssertionException (string message)
		 : base (message)
		{ }
	}
}
