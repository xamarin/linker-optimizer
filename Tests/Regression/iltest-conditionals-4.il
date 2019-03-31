.assembly 'iltest-conditionals-4'
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

			call class Martin.LinkerTest.Foo class Martin.LinkerTest.TestConditionals::ConvertToReturnNull ()
			brfalse label2
		
		label1:
			call class [mscorlib]System.Exception class [TestHelpers]Martin.LinkerTest.TestHelpers::AssertRemoved ()
			throw

		label2:
			call class Martin.LinkerTest.Foo class Martin.LinkerTest.TestConditionals::ConvertToReturnNull ()
			brtrue label1

		label3:
			newobj instance void class Martin.LinkerTest.TestConditionals::'.ctor' ()
			stloc.0

			ldloc.0
			callvirt instance class Martin.LinkerTest.TestConditionals class Martin.LinkerTest.TestConditionals::GetInstance ()
			call instance object class Martin.LinkerTest.TestConditionals::ConvertToInstanceReturnNull ()
			brtrue label1

		label4:
			ret
		}

		.method public hidebysig static default class Martin.LinkerTest.Foo ConvertToReturnNull () cil managed noinlining
		{
			newobj instance void class Martin.LinkerTest.Foo::'.ctor' ()
			dup
			call instance void class Martin.LinkerTest.Foo::Hello ()
			ret
		}

		.method public hidebysig instance default object ConvertToInstanceReturnNull () cil managed noinlining
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
