using System;
using System.Reflection;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Martin.LinkerTest
{
	static class TestRemoting
	{
		public static void Main ()
		{
			var foo = (Foo)Activator.CreateInstance (typeof (Foo));
			foo.Hello ();

			var foo2 = (Foo)Activator.CreateInstance (typeof (Foo), BindingFlags.Public | BindingFlags.Instance, null, new object[] { "Boston" }, CultureInfo.InvariantCulture);
			foo2.Hello ();
		}
	}

	class Foo
	{
		public readonly string Name;

		public Foo ()
		{
			Name = "World";
		}

		public Foo (string name)
		{
			Name = name;
		}

		public void Hello ()
		{
			Console.WriteLine ($"Hello {Name}!");
		}
	}
}
