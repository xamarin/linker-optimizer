using System;
using System.Runtime.CompilerServices;

namespace Martin.LinkerTest
{
	public static class TestHelpers
	{
		public static void Assert (bool condition, [CallerMemberName] string caller = null)
		{
			if (condition)
				return;
			if (!string.IsNullOrEmpty (caller))
				throw new AssertionException ($"Assertion failed at {caller}");
			throw new AssertionException ("Assertion failed");
		}

		public static void AssertFail (string message, [CallerMemberName] string caller = null)
		{
			if (!string.IsNullOrEmpty (caller))
				throw new AssertionException ($"Assertion failed at {caller}: {message}");
			throw new AssertionException ($"Assertion failed: {message}");
		}

		public static void AssertEqual (int expected, int actual, string message = null, [CallerMemberName] string caller = null)
		{
			if (actual == expected)
				return;
			throw new AssertionException ($"Assertion failed (expected {expected}, got {actual}){(caller != null ? " at " + caller : "")}{(message != null ? ": " + message : "")}.");
		}

		public static void AssertNotNull (object instance, string message = null, [CallerMemberName] string caller = null)
		{
			if (instance != null)
				return;
			throw new AssertionException ($"Assertion failed (expected non-null instance){(caller != null ? " at " + caller : "")}{(message != null ? ": " + message : "")}.");
		}

		/*
		 * We scan the generated output for any references to this method and make the test fail.
		 */
		public static Exception AssertRemoved ()
		{
			throw new AssertionException ("This code should have been removed.");
		}

		public static void Debug ([CallerMemberName] string caller = null)
		{
			if (!string.IsNullOrEmpty (caller))
				Console.Error.WriteLine ($"TEST DEBUG AT {caller}");
			else
				Console.Error.WriteLine ($"TEST DEBUG");
		}

		public static void Debug (string message, [CallerMemberName] string caller = null)
		{
			if (!string.IsNullOrEmpty (caller))
				Console.Error.WriteLine ($"TEST DEBUG AT {caller}: {message}");
			else
				Console.Error.WriteLine ($"TEST DEBUG: {message}");
		}
	}
}
