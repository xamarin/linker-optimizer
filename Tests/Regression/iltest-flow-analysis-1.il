.assembly 'iltest-flow-analysis-1'
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
		.method public static hidebysig default void Main () cil managed
		{
			.entrypoint
			call int32 class Martin.LinkerTest.TestConditionals::FlowAnalysis1 ()
			ldc.i4.3
			beq test2

			ldstr "FlowAnalysis1() should have returned 3"
			newobj instance void class [mscorlib]System.InvalidOperationException::'.ctor' (string)
			throw

		test2:
			ldc.i4.0
			newarr [mscorlib]System.String
			call bool class Martin.LinkerTest.TestConditionals::FlowAnalysis2 (string[])
			pop

			call bool class Martin.LinkerTest.TestConditionals::FlowAnalysis3 ()
			pop

			ret
		}

		.method private static hidebysig default int32 FlowAnalysis1 () cil managed
		{
			.locals init (int32 test)

			br end

		unreachable:
			call [mscorlib]System.Exception class [TestHelpers] Martin.LinkerTest.TestHelpers::AssertRemoved ()
			pop

		loop:
			ldloc.0
			ldc.i4.2
			add
			stloc.0
			br return

		end:
			ldloc.0
			ldc.i4.1
			add
			stloc.0
			br loop

		return:
			ldloc.0
			ret
		}

		.method private static hidebysig default bool FlowAnalysis2 (string[] 'array') cil managed
		{
			.maxstack 3
			.locals init (
				int32	V_0,
				int32	V_1)

			ldc.i4.0
			stloc.0
			br.s label5

		label1:
			ldc.i4.0
			stloc.1
			br.s label4

		label2:
			ldarg.0
			ldloc.0
			ldelem.ref
			ldloc.1
			callvirt instance char string::get_Chars (int32)
			call bool char::IsWhiteSpace (char)
			brfalse.s label3

			ldc.i4.1
			ret

		label3:
			ldloc.1
			ldc.i4.1
			add
			stloc.1

		label4:
			ldloc.1
			ldarg.0
			ldloc.0
			ldelem.ref
			callvirt instance int32 string::get_Length ()
			blt.s label2

			ldloc.0
			ldc.i4.1
			add
			stloc.0

		label5:
			ldloc.0
			ldarg.0
			ldlen
			conv.i4
			blt.s label1

			ldc.i4.0
			ret
		}

		.method private static hidebysig default bool FlowAnalysis3 () cil managed
		{
			.maxstack 1
			.locals init (
				bool	V_0)

			.try {
				newobj instance void class [mscorlib]System.NotSupportedException::'.ctor' ()
				throw
			} catch class [mscorlib]System.Object {
				pop
				ldc.i4.0
				stloc.0
				leave.s end
			}

		end:
			ldloc.0
			ret
		}
	}
}
