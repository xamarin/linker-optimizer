.assembly 'iltest-conditionals-3'
{
}

.assembly extern TestHelpers
{
	.ver 0:0:0:0
}

.namespace Martin.LinkerTest
{
	.class private auto ansi sealed beforefieldinit TestConditionals
		extends [mscorlib]System.Object
	{
		.method public static hidebysig default void Main ()  cil managed 
		{
			.entrypoint

			.locals init (class Martin.LinkerTest.TestConditionals V_0)

			.try {
				call void class Martin.LinkerTest.TestConditionals::ConvertToThrow ()
				call class [mscorlib]System.Exception class [TestHelpers]Martin.LinkerTest.TestHelpers::AssertRemoved ()
				pop
				leave label1
			} catch class [mscorlib]System.PlatformNotSupportedException {
				pop
				leave label1
			}

		label1:
			newobj instance void class Martin.LinkerTest.TestConditionals::'.ctor' ()
			stloc.0

			.try {
				ldloc.0
				callvirt instance object class Martin.LinkerTest.TestConditionals::ConvertToInstanceThrow ()
				pop
				call class [mscorlib]System.Exception class [TestHelpers]Martin.LinkerTest.TestHelpers::AssertRemoved ()
				pop
				leave label2
			} catch class [mscorlib]System.PlatformNotSupportedException {
				pop
				leave label2
			}

		label2:
			.try {
				ldloc.0
				callvirt instance class Martin.LinkerTest.TestConditionals class Martin.LinkerTest.TestConditionals::GetInstance ()
				callvirt instance object class Martin.LinkerTest.TestConditionals::ConvertToInstanceThrow ()
				pop
				call class [mscorlib]System.Exception class [TestHelpers]Martin.LinkerTest.TestHelpers::AssertRemoved ()
				pop
				leave label3
			} catch class [mscorlib]System.PlatformNotSupportedException {
				pop
				leave label3
			}

		label3:
			.try {
				call class Martin.LinkerTest.Foo class Martin.LinkerTest.TestConditionals::ConvertToFooThrow ()
				call class [mscorlib]System.Exception class [TestHelpers]Martin.LinkerTest.TestHelpers::AssertRemoved ()
				pop
				leave label4
			} catch class [mscorlib]System.PlatformNotSupportedException {
				pop
				leave label4
			}

		label4:
			ret
		}

		.method public static hidebysig default void ConvertToThrow () cil managed noinlining
		{
			call class [mscorlib]System.Exception class [TestHelpers]Martin.LinkerTest.TestHelpers::AssertRemoved ()
			pop
			ret
		}

		.method public hidebysig instance default object ConvertToInstanceThrow () cil managed noinlining
		{
			call class [mscorlib]System.Exception class [TestHelpers]Martin.LinkerTest.TestHelpers::AssertRemoved ()
			throw
		}

		.method public hidebysig static default class Martin.LinkerTest.Foo ConvertToFooThrow () cil managed noinlining
		{
			newobj instance void class Martin.LinkerTest.Foo::'.ctor' ()
			dup
			call instance void class Martin.LinkerTest.Foo::Hello ()
			ret
		}

		.method public hidebysig instance default class Martin.LinkerTest.TestConditionals GetInstance () cil managed noinlining
		{
			ldarg.0
			ret
		}

		.method public hidebysig specialname rtspecialname instance default void '.ctor' () cil managed
		{
			ldarg.0
			call instance void object::'.ctor' ()
			ret
		}
	}

	.class private auto ansi beforefieldinit Foo extends [mscorlib]System.Object
	{
		.method public hidebysig default void Hello () cil managed
		{
			call class [mscorlib]System.Exception class [TestHelpers]Martin.LinkerTest.TestHelpers::AssertRemoved ()
			pop
			ret
		}

		.method public hidebysig specialname rtspecialname instance default void '.ctor' () cil managed
		{
			ldarg.0
			call instance void object::'.ctor'()
			ret
		}
	}
}
