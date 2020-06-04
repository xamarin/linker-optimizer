using System;

namespace SharedTests.SharedSample
{
	public static class Variables
	{
		public static void VariableTest ()
		{ // @@BEGIN-SCOPE
			var test = "Hello World";
			var piOverE = Math.PI / Math.E;
			var message = $"PI divided by E: {piOverE}";
			var obj = new MyObject ();
			var exc = new MyException ("Throwing here.", 99999);

			// @@BREAKPOINT: VariableTest
			Console.WriteLine (obj);
			Console.WriteLine (obj.PiAtThePowerOfE);
			Console.WriteLine (piOverE);
			Console.WriteLine (message);
			Console.WriteLine (test);
			Console.WriteLine (exc);
		} // @@END-SCOPE

		public static string GetVersion () => Environment.Version.ToString ();

		public static string GetOSVersion () => Environment.OSVersion.ToString ();
	}
}
