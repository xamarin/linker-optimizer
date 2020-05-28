using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace WebBindings
{
	public class Hello
	{
		public static void Main ()
		{
			Console.WriteLine ("Hello World!");
		}

		public static async void Request ()
		{
			var client = new HttpClient ();
			var result = await client.GetAsync ("http://www.microsoft.com/").ConfigureAwait (false);
			Console.Error.WriteLine ($"GOT RESULT: {result}");
		}
	}
}
