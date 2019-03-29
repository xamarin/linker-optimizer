.assembly 'iltest-conditionals-2'
{
}

.assembly extern TestHelpers
{
	.ver 0:0:0:0
}

.namespace Martin.LinkerTest
{
	.class private auto ansi abstract sealed beforefieldinit TestConditionals
		extends [mscorlib]System.Object
	{
		.method public static hidebysig default void Main ()  cil managed 
		{
			.entrypoint

			.try {
				call void class Martin.LinkerTest.TestConditionals::RunFeature1 ()
				newobj instance void class [TestHelpers]Martin.LinkerTest.AssertionException::'.ctor' ()
				throw
			} catch class [mscorlib]System.PlatformNotSupportedException {
				pop
				leave feature2
			}

		feature2:
			call void class Martin.LinkerTest.TestConditionals::RunFeature2 ()
			call void class Martin.LinkerTest.TestConditionals::RunFeature3 ()
			ret
		}

		.method private static hidebysig default void RunFeature1 ()  cil managed noinlining
		{
			ldc.i4.0
			call void class [TestHelpers]System.Runtime.CompilerServices.MonoLinkerSupport::RequireFeature(valuetype [TestHelpers]System.Runtime.CompilerServices.MonoLinkerFeature)
			call [mscorlib]System.Exception class [TestHelpers]Martin.LinkerTest.TestHelpers::AssertRemoved ()
			throw
		}

		.method private static hidebysig default void RunFeature2 ()  cil managed noinlining
		{
			ldc.i4.3
			call void class [TestHelpers]System.Runtime.CompilerServices.MonoLinkerSupport::RequireFeature(valuetype [TestHelpers]System.Runtime.CompilerServices.MonoLinkerFeature)
			ret
		}

		.method private static hidebysig default void RunFeature3 ()  cil managed noinlining
		{
			br start

			ldc.i4.0
			pop

		start:
			ldc.i4.3
			call void class [TestHelpers]System.Runtime.CompilerServices.MonoLinkerSupport::RequireFeature(valuetype [TestHelpers]System.Runtime.CompilerServices.MonoLinkerFeature)
			ret
		}

	}
}
