.assembly 'iltest-conditionals-1'
{
}
.assembly extern 'TestHelpers'
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
			call bool class Martin.LinkerTest.TestConditionals::RunFeature1 ()
			brtrue feature2

			ldstr "RunFeature1() should have returned true"
			newobj instance void class [mscorlib]System.InvalidOperationException::'.ctor'(string)
			throw

		feature2:
			call void class Martin.LinkerTest.TestConditionals::RunFeature2 ()

			ldc.i4.3
			call int32 class Martin.LinkerTest.TestConditionals::RunFeature3 ()
			ldnull
			ldnull
			call void class [TestHelpers]Martin.LinkerTest.TestHelpers::AssertEqual (int32 expected, int32 actual, [opt] string message, [opt] string caller)

			ldc.i4 11
			call int32 class Martin.LinkerTest.TestConditionals::RunFeature4 ()
			ldnull
			ldnull
			call void class [TestHelpers]Martin.LinkerTest.TestHelpers::AssertEqual (int32 expected, int32 actual, [opt] string message, [opt] string caller)

			ldc.i4 12
			call int32 class Martin.LinkerTest.TestConditionals::RunFeature5 ()
			ldnull
			ldnull
			call void class [TestHelpers]Martin.LinkerTest.TestHelpers::AssertEqual (int32 expected, int32 actual, [opt] string message, [opt] string caller)

			ret 
		}

		.method private static hidebysig default bool RunFeature1 () cil managed
		{
			.locals init (int32 test, bool result)

			// Make sure `result` is initialized to prevent it from being turned into a constant.
			// In C#, definite assignment rules prevent this from happening.
			ldc.i4.0
			stloc.1

			ldc.i4.0 // MonoLinkerFeature.Unknown
			call bool class [TestHelpers]System.Runtime.CompilerServices.MonoLinkerSupport::IsFeatureSupported (valuetype [TestHelpers]System.Runtime.CompilerServices.MonoLinkerFeature)
			brtrue return

		conditional:
			ldc.i4.4
			stloc.0
		
		loop:
			ldloc.0
			ldc.i4.1
			sub
			dup
			stloc.0
			ldc.i4.1
			stloc.1
			brtrue loop

		return:
			ldloc.1
			ret
		}

		.method private static hidebysig default void RunFeature2 () cil managed
		{
			.locals init (int32 test)

			ldc.i4.0 // MonoLinkerFeature.Unknown
			call bool class [TestHelpers]System.Runtime.CompilerServices.MonoLinkerSupport::IsFeatureSupported (valuetype [TestHelpers]System.Runtime.CompilerServices.MonoLinkerFeature)
			brfalse return

		conditional:
			ldc.i4.4
			stloc.0
			call class [mscorlib]System.Exception class [TestHelpers]Martin.LinkerTest.TestHelpers::AssertRemoved ()

		loop:
			ldloc.0
			ldc.i4.1
			sub
			dup
			stloc.0
			brtrue loop

		return:
			ret
		}

		.method private static hidebysig default int32 RunFeature3 () cil managed
		{
			.locals init (int32 test, int32 result)

			ldc.i4.3
			stloc.0

			ldc.i4.0
			stloc.1

			ldc.i4.0 // MonoLinkerFeature.Unknown
			call bool class [TestHelpers]System.Runtime.CompilerServices.MonoLinkerSupport::IsFeatureSupported (valuetype [TestHelpers]System.Runtime.CompilerServices.MonoLinkerFeature)
			brfalse return

		conditional:
			ldc.i4.8
			ldloc.1
			add
			stloc.1
			call class [mscorlib]System.Exception class [TestHelpers]Martin.LinkerTest.TestHelpers::AssertRemoved ()
			pop
		
		loop:
			ldc.i4.1
			ldloc.1
			add
			stloc.1

		loop1:
			ldloc.0
			ldc.i4.1
			sub
			dup
			stloc.0
			brtrue loop

		return:
			ldloc.0
			brtrue loop

			ldloc.1
			ret
		}

		.method private static hidebysig default int32 RunFeature4 () cil managed
		{
			.locals init (int32 test, int32 result)

			ldc.i4.3
			stloc.0

			ldc.i4.0
			stloc.1

			ldc.i4.1 // MonoLinkerFeature.Martin
			call bool class [TestHelpers]System.Runtime.CompilerServices.MonoLinkerSupport::IsFeatureSupported (valuetype [TestHelpers]System.Runtime.CompilerServices.MonoLinkerFeature)
			brfalse return

		conditional:
			ldc.i4.8
			ldloc.1
			add
			stloc.1
		
		loop:
			ldc.i4.1
			ldloc.1
			add
			stloc.1

		loop1:
			ldloc.0
			ldc.i4.1
			sub
			dup
			stloc.0
			brtrue loop

		return:
			ldloc.0
			brtrue loop

			ldloc.1
			ret
		}

		.method private static hidebysig default int32 RunFeature5 () cil managed
		{
			.locals init (int32 test, int32 return)

			ldc.i4.8
			stloc.1

			ldc.i4.3
			stloc.0

			ldloc 0
			stloc.s 0

			ldloca 0
			pop

			ldc.i4.1 // MonoLinkerFeature.Martin
			call bool class [TestHelpers]System.Runtime.CompilerServices.MonoLinkerSupport::IsFeatureSupported (valuetype [TestHelpers]System.Runtime.CompilerServices.MonoLinkerFeature)
			brfalse return

		conditional:
			br start
		
		loop:
			ldc.i4.1
			ldloc.1
			add
			stloc.1
			ldloc.0
			ldc.i4.1
			sub
			dup
			stloc.0
			brtrue loop
			ldloc.1
			ret

		start:
			ldc.i4.4
			stloc.0
			br loop

		return:
			call class [mscorlib]System.Exception class [TestHelpers]Martin.LinkerTest.TestHelpers::AssertRemoved ()
			pop
			ldloc.1
			ret
		}			
	}
}
