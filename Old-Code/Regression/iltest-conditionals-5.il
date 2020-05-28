.assembly 'iltest-conditionals-5'
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

			call bool class Martin.LinkerTest.TestConditionals::ConvertToReturnFalse ()
			brfalse label2
		
		label1:
			call class [mscorlib]System.Exception class [TestHelpers]Martin.LinkerTest.TestHelpers::AssertRemoved ()
			throw

		label2:
			call bool class Martin.LinkerTest.TestConditionals::ConvertToReturnFalse ()
			brtrue label1

		label3:
			newobj instance void class Martin.LinkerTest.TestConditionals::'.ctor' ()
			stloc.0

			ldloc.0
			callvirt instance class Martin.LinkerTest.TestConditionals class Martin.LinkerTest.TestConditionals::GetInstance ()
			call instance bool class Martin.LinkerTest.TestConditionals::ConvertToInstanceReturnFalse ()
			brtrue label1

		label4:
			ret
		}

		.method public hidebysig static default bool ConvertToReturnFalse () cil managed noinlining
		{
			ldc.i4.1
			ret
		}

		.method public hidebysig instance default bool ConvertToInstanceReturnFalse () cil managed noinlining
		{
			ldc.i4.1
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
