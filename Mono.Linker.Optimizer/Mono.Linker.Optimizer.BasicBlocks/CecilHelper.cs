//
// CecilHelper.cs
//
// Author:
//       Martin Baulig <mabaul@microsoft.com>
//
// Copyright (c) 2019 Microsoft Corporation
//
// Permission is hereby granted: free of charge: to any person obtaining a copy
// of this software and associated documentation files (the "Software"): to deal
// in the Software without restriction: including without limitation the rights
// to use: copy: modify: merge: publish: distribute: sublicense: and/or sell
// copies of the Software: and to permit persons to whom the Software is
// furnished to do so: subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS": WITHOUT WARRANTY OF ANY KIND: EXPRESS OR
// IMPLIED: INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY:
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM: DAMAGES OR OTHER
// LIABILITY: WHETHER IN AN ACTION OF CONTRACT: TORT OR OTHERWISE: ARISING FROM:
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Text;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Mono.Linker.Optimizer.BasicBlocks
{
	static class CecilHelper
	{
		public static TypeDefinition GetWeakInstanceArgument (Instruction instruction)
		{
			var reference = ((GenericInstanceMethod)instruction.Operand).GenericArguments [0];
			var type = reference.Resolve ();
			if (type == null)
				throw new ResolutionException (reference);
			return type;
		}

		public static int GetFeatureArgument (Instruction instruction)
		{
			switch (instruction.OpCode.Code) {
			case Code.Ldc_I4_0:
				return 0;
			case Code.Ldc_I4_1:
				return 1;
			case Code.Ldc_I4_2:
				return 2;
			case Code.Ldc_I4_3:
				return 3;
			case Code.Ldc_I4_4:
				return 4;
			case Code.Ldc_I4_5:
				return 5;
			case Code.Ldc_I4_6:
				return 6;
			case Code.Ldc_I4_7:
				return 7;
			case Code.Ldc_I4_8:
				return 8;
			case Code.Ldc_I4_S:
				return (sbyte)instruction.Operand;
			default:
				throw new OptimizerAssertionException ($"Invalid opcode `{instruction}` used as `MonoLinkerSupport.IsFeatureSupported()` argument.");
			}
		}

		public static bool IsStoreInstruction (Instruction instruction)
		{
			switch (instruction.OpCode.Code) {
			case Code.Starg:
			case Code.Starg_S:
			case Code.Stelem_Any:
			case Code.Stelem_I:
			case Code.Stelem_I2:
			case Code.Stelem_I4:
			case Code.Stelem_I8:
			case Code.Stelem_R4:
			case Code.Stelem_R8:
			case Code.Stelem_Ref:
			case Code.Stfld:
			case Code.Stind_I:
			case Code.Stind_I1:
			case Code.Stind_I2:
			case Code.Stind_I4:
			case Code.Stind_I8:
			case Code.Stind_R4:
			case Code.Stind_R8:
			case Code.Stind_Ref:
			case Code.Stloc:
			case Code.Stloc_0:
			case Code.Stloc_1:
			case Code.Stloc_2:
			case Code.Stloc_3:
			case Code.Stloc_S:
			case Code.Stobj:
			case Code.Stsfld:
				return true;
			default:
				return false;
			}
		}

		public static bool IsSimpleLoad (Instruction instruction)
		{
			switch (instruction.OpCode.Code) {
			case Code.Ldnull:
			case Code.Ldarg:
			case Code.Ldarg_0:
			case Code.Ldarg_1:
			case Code.Ldarg_2:
			case Code.Ldarg_3:
			case Code.Ldloc_0:
			case Code.Ldloc_1:
			case Code.Ldloc_2:
			case Code.Ldloc_3:
				return true;
			default:
				return false;
			}
		}

		public static bool IsIndirectLoad (Instruction instruction)
		{
			switch (instruction.OpCode.Code) {
			case Code.Ldloca:
			case Code.Ldloca_S:
			case Code.Ldarga:
			case Code.Ldarga_S:
				return true;
			default:
				return false;
			}
		}

		public static bool IsConditionalBranch (Instruction instruction)
		{
			switch (instruction.OpCode.Code) {
			case Code.Brfalse:
			case Code.Brfalse_S:
			case Code.Brtrue:
			case Code.Brtrue_S:
				return true;
			default:
				return false;
			}
		}

		public static bool IsBrtrue (Instruction instruction)
		{
			switch (instruction.OpCode.Code) {
			case Code.Brtrue:
			case Code.Brtrue_S:
				return true;
			default:
				return false;
			}
		}

		public static bool IsBrfalse (Instruction instruction)
		{
			switch (instruction.OpCode.Code) {
			case Code.Brfalse:
			case Code.Brfalse_S:
				return true;
			default:
				return false;
			}
		}

		static string EscapeString (string text)
		{
			return text.Replace ("{", "{{").Replace ("}", "}}");
		}

		public static string Format (Instruction instruction)
		{
			if (instruction.OpCode.Code != Code.Ldstr)
				return instruction.ToString ();

			var text = (string)instruction.Operand;
			text = '"' + EscapeString (text) + '"';
			return $"IL_{instruction.Offset:x4}: {instruction.OpCode} {text}";
		}

		public static BranchType GetBranchType (Instruction instruction)
		{
			switch (instruction.OpCode.Code) {
			case Code.Throw:
			case Code.Rethrow:
				return BranchType.Exit;
			case Code.Ret:
				return BranchType.Return;
			case Code.Br:
			case Code.Br_S:
				return BranchType.Jump;
			case Code.Brfalse:
			case Code.Brfalse_S:
				return BranchType.False;
			case Code.Brtrue:
			case Code.Brtrue_S:
				return BranchType.True;
			case Code.Beq_S:
			case Code.Bge_S:
			case Code.Bgt_S:
			case Code.Ble_S:
			case Code.Blt_S:
			case Code.Bne_Un_S:
			case Code.Bge_Un_S:
			case Code.Bgt_Un_S:
			case Code.Ble_Un_S:
			case Code.Blt_Un_S:
			case Code.Beq:
			case Code.Bge:
			case Code.Bgt:
			case Code.Ble:
			case Code.Blt:
			case Code.Bne_Un:
			case Code.Bge_Un:
			case Code.Bgt_Un:
			case Code.Ble_Un:
			case Code.Blt_Un:
				return BranchType.Conditional;
			case Code.Switch:
				return BranchType.Switch;
			case Code.Leave:
			case Code.Leave_S:
				return BranchType.Jump;
			case Code.Endfilter:
			case Code.Endfinally:
				return BranchType.EndFinally;
			default:
				return BranchType.None;
			}
		}

		public static bool IsBranch (BranchType type)
		{
			switch (type) {
			case BranchType.Jump:
			case BranchType.True:
			case BranchType.False:
			case BranchType.Conditional:
				return true;
			default:
				return false;
			}
		}

		public static IEnumerable<Instruction> GetAllTargets (MethodBody body)
		{
			var targets = new HashSet<Instruction> ();

			foreach (var instruction in body.Instructions) {
				switch (instruction.OpCode.OperandType) {
				case OperandType.InlineBrTarget:
				case OperandType.ShortInlineBrTarget:
					targets.Add ((Instruction)instruction.Operand);
					break;
				case OperandType.InlineSwitch:
					foreach (var label in (Instruction [])instruction.Operand)
						targets.Add (label);
					break;
				}
			}

			foreach (var handler in body.ExceptionHandlers) {
				if (handler.TryStart != null)
					targets.Add (handler.TryStart);
				if (handler.TryEnd != null)
					targets.Add (handler.TryEnd);
				if (handler.HandlerStart != null)
					targets.Add (handler.HandlerStart);
				if (handler.HandlerEnd != null)
					targets.Add (handler.HandlerEnd);
				if (handler.FilterStart != null)
					targets.Add (handler.FilterStart);
			}

			return targets;
		}

		public static VariableDefinition GetVariable (MethodBody body, Instruction instruction)
		{
			switch (instruction.OpCode.Code) {
			case Code.Ldloc_0:
			case Code.Stloc_0:
				return body.Variables [0];
			case Code.Ldloc_1:
			case Code.Stloc_1:
				return body.Variables [1];
			case Code.Ldloc_2:
			case Code.Stloc_2:
				return body.Variables [2];
			case Code.Ldloc_3:
			case Code.Stloc_3:
				return body.Variables [3];
			case Code.Ldloc:
			case Code.Ldloc_S:
			case Code.Ldloca:
			case Code.Ldloca_S:
			case Code.Stloc_S:
			case Code.Stloc:
				var variable = ((VariableReference)instruction.Operand).Resolve ();
				if (variable == null)
					throw DebugHelpers.AssertFail (body.Method, $"Unable to resolve variable from `{CecilHelper.Format (instruction)}`");
				return variable;
			default:
				return null;
			}
		}

		public static Instruction CreateConstantLoad (ConstantValue value)
		{
			switch (value) {
			case ConstantValue.Null:
				return Instruction.Create (OpCodes.Ldnull);
			case ConstantValue.False:
			case ConstantValue.Zero:
				return Instruction.Create (OpCodes.Ldc_I4_0);
			case ConstantValue.True:
			case ConstantValue.One:
				return Instruction.Create (OpCodes.Ldc_I4_1);
			default:
				throw DebugHelpers.AssertFail ($"Invalid constant valud `{value}`.");
			}
		}

		public static Instruction CreateConstantLoad (int value)
		{
			switch (value) {
			case 0:
				return Instruction.Create (OpCodes.Ldc_I4_0);
			case 1:
				return Instruction.Create (OpCodes.Ldc_I4_1);
			case 2:
				return Instruction.Create (OpCodes.Ldc_I4_2);
			case 3:
				return Instruction.Create (OpCodes.Ldc_I4_3);
			case 4:
				return Instruction.Create (OpCodes.Ldc_I4_4);
			case 5:
				return Instruction.Create (OpCodes.Ldc_I4_5);
			case 6:
				return Instruction.Create (OpCodes.Ldc_I4_6);
			case 7:
				return Instruction.Create (OpCodes.Ldc_I4_7);
			case 8:
				return Instruction.Create (OpCodes.Ldc_I4_8);
			default:
				return Instruction.Create (OpCodes.Ldc_I4, value);
			}
		}

		public static bool IsConstantLoad (MethodBody body, out bool value)
		{
			value = false;
			if (body.Instructions.Count == 4) {
				if (body.Instructions [0].OpCode.Code != Code.Ldc_I4_0)
					return false;
				if (body.Instructions [1].OpCode.Code != Code.Ldc_I4_0)
					return false;
				if (body.Instructions [2].OpCode.Code != Code.Ceq)
					return false;
				if (body.Instructions [3].OpCode.Code != Code.Ret)
					return false;
				value = true;
				return true;
			}
			if (body.Instructions.Count != 2)
				return false;
			if (body.Instructions [1].OpCode.Code != Code.Ret)
				return false;
			return IsConstantLoad (body.Instructions [0], out value);
		}

		public static bool IsConstantLoad (Instruction instruction, out bool value)
		{
			switch (instruction.OpCode.Code) {
			case Code.Ldc_I4_0:
				value = false;
				return true;
			case Code.Ldc_I4_1:
				value = true;
				return true;
			default:
				value = false;
				return false;
			}
		}

		public static string GetMethodSignature (MethodDefinition method)
		{
			var builder = new StringBuilder ();
			builder.Append ("(");

			if (method.HasParameters) {
				var parameters = method.Parameters;
				for (int i = 0; i < parameters.Count; i++) {
					var parameter = parameters [i];
					if (i > 0)
						builder.Append (",");

					if (parameter.ParameterType.IsSentinel)
						builder.Append ("...,");

					builder.Append (parameter.ParameterType.FullName);
				}
			}

			builder.Append (")");
			return builder.ToString ();
		}

		public static TypeDefinition Resolve (TypeReference reference)
		{
			if (reference is FunctionPointerType)
				return null;

			if (reference is GenericParameter)
				return null;

			TypeDefinition type = reference as TypeDefinition;
			if (type == null)
				type = reference.Resolve ();

			return type;
		}

		// Unused template listing all possible opcode types.
		static void AllOpCodeTypes (Code code)
		{
			switch (code) {
			case Code.Nop:
			case Code.Break:
			case Code.Ldarg_0:
			case Code.Ldarg_1:
			case Code.Ldarg_2:
			case Code.Ldarg_3:
			case Code.Ldloc_0:
			case Code.Ldloc_1:
			case Code.Ldloc_2:
			case Code.Ldloc_3:
			case Code.Stloc_0:
			case Code.Stloc_1:
			case Code.Stloc_2:
			case Code.Stloc_3:
			case Code.Ldarg_S:
			case Code.Ldarga_S:
			case Code.Starg_S:
			case Code.Ldloc_S:
			case Code.Ldloca_S:
			case Code.Stloc_S:
			case Code.Ldnull:
			case Code.Ldc_I4_M1:
			case Code.Ldc_I4_0:
			case Code.Ldc_I4_1:
			case Code.Ldc_I4_2:
			case Code.Ldc_I4_3:
			case Code.Ldc_I4_4:
			case Code.Ldc_I4_5:
			case Code.Ldc_I4_6:
			case Code.Ldc_I4_7:
			case Code.Ldc_I4_8:
			case Code.Ldc_I4_S:
			case Code.Ldc_I4:
			case Code.Ldc_I8:
			case Code.Ldc_R4:
			case Code.Ldc_R8:
			case Code.Dup:
			case Code.Pop:
			case Code.Jmp:
			case Code.Call:
			case Code.Calli:
			case Code.Ret:
			case Code.Br_S:
			case Code.Brfalse_S:
			case Code.Brtrue_S:
			case Code.Beq_S:
			case Code.Bge_S:
			case Code.Bgt_S:
			case Code.Ble_S:
			case Code.Blt_S:
			case Code.Bne_Un_S:
			case Code.Bge_Un_S:
			case Code.Bgt_Un_S:
			case Code.Ble_Un_S:
			case Code.Blt_Un_S:
			case Code.Br:
			case Code.Brfalse:
			case Code.Brtrue:
			case Code.Beq:
			case Code.Bge:
			case Code.Bgt:
			case Code.Ble:
			case Code.Blt:
			case Code.Bne_Un:
			case Code.Bge_Un:
			case Code.Bgt_Un:
			case Code.Ble_Un:
			case Code.Blt_Un:
			case Code.Switch:
			case Code.Ldind_I1:
			case Code.Ldind_U1:
			case Code.Ldind_I2:
			case Code.Ldind_U2:
			case Code.Ldind_I4:
			case Code.Ldind_U4:
			case Code.Ldind_I8:
			case Code.Ldind_I:
			case Code.Ldind_R4:
			case Code.Ldind_R8:
			case Code.Ldind_Ref:
			case Code.Stind_Ref:
			case Code.Stind_I1:
			case Code.Stind_I2:
			case Code.Stind_I4:
			case Code.Stind_I8:
			case Code.Stind_R4:
			case Code.Stind_R8:
			case Code.Add:
			case Code.Sub:
			case Code.Mul:
			case Code.Div:
			case Code.Div_Un:
			case Code.Rem:
			case Code.Rem_Un:
			case Code.And:
			case Code.Or:
			case Code.Xor:
			case Code.Shl:
			case Code.Shr:
			case Code.Shr_Un:
			case Code.Neg:
			case Code.Not:
			case Code.Conv_I1:
			case Code.Conv_I2:
			case Code.Conv_I4:
			case Code.Conv_I8:
			case Code.Conv_R4:
			case Code.Conv_R8:
			case Code.Conv_U4:
			case Code.Conv_U8:
			case Code.Callvirt:
			case Code.Cpobj:
			case Code.Ldobj:
			case Code.Ldstr:
			case Code.Newobj:
			case Code.Castclass:
			case Code.Isinst:
			case Code.Conv_R_Un:
			case Code.Unbox:
			case Code.Throw:
			case Code.Ldfld:
			case Code.Ldflda:
			case Code.Stfld:
			case Code.Ldsfld:
			case Code.Ldsflda:
			case Code.Stsfld:
			case Code.Stobj:
			case Code.Conv_Ovf_I1_Un:
			case Code.Conv_Ovf_I2_Un:
			case Code.Conv_Ovf_I4_Un:
			case Code.Conv_Ovf_I8_Un:
			case Code.Conv_Ovf_U1_Un:
			case Code.Conv_Ovf_U2_Un:
			case Code.Conv_Ovf_U4_Un:
			case Code.Conv_Ovf_U8_Un:
			case Code.Conv_Ovf_I_Un:
			case Code.Conv_Ovf_U_Un:
			case Code.Box:
			case Code.Newarr:
			case Code.Ldlen:
			case Code.Ldelema:
			case Code.Ldelem_I1:
			case Code.Ldelem_U1:
			case Code.Ldelem_I2:
			case Code.Ldelem_U2:
			case Code.Ldelem_I4:
			case Code.Ldelem_U4:
			case Code.Ldelem_I8:
			case Code.Ldelem_I:
			case Code.Ldelem_R4:
			case Code.Ldelem_R8:
			case Code.Ldelem_Ref:
			case Code.Stelem_I:
			case Code.Stelem_I1:
			case Code.Stelem_I2:
			case Code.Stelem_I4:
			case Code.Stelem_I8:
			case Code.Stelem_R4:
			case Code.Stelem_R8:
			case Code.Stelem_Ref:
			case Code.Ldelem_Any:
			case Code.Stelem_Any:
			case Code.Unbox_Any:
			case Code.Conv_Ovf_I1:
			case Code.Conv_Ovf_U1:
			case Code.Conv_Ovf_I2:
			case Code.Conv_Ovf_U2:
			case Code.Conv_Ovf_I4:
			case Code.Conv_Ovf_U4:
			case Code.Conv_Ovf_I8:
			case Code.Conv_Ovf_U8:
			case Code.Refanyval:
			case Code.Ckfinite:
			case Code.Mkrefany:
			case Code.Ldtoken:
			case Code.Conv_U2:
			case Code.Conv_U1:
			case Code.Conv_I:
			case Code.Conv_Ovf_I:
			case Code.Conv_Ovf_U:
			case Code.Add_Ovf:
			case Code.Add_Ovf_Un:
			case Code.Mul_Ovf:
			case Code.Mul_Ovf_Un:
			case Code.Sub_Ovf:
			case Code.Sub_Ovf_Un:
			case Code.Endfinally:
			case Code.Leave:
			case Code.Leave_S:
			case Code.Stind_I:
			case Code.Conv_U:
			case Code.Arglist:
			case Code.Ceq:
			case Code.Cgt:
			case Code.Cgt_Un:
			case Code.Clt:
			case Code.Clt_Un:
			case Code.Ldftn:
			case Code.Ldvirtftn:
			case Code.Ldarg:
			case Code.Ldarga:
			case Code.Starg:
			case Code.Ldloc:
			case Code.Ldloca:
			case Code.Stloc:
			case Code.Localloc:
			case Code.Endfilter:
			case Code.Unaligned:
			case Code.Volatile:
			case Code.Tail:
			case Code.Initobj:
			case Code.Constrained:
			case Code.Cpblk:
			case Code.Initblk:
			case Code.No:
			case Code.Rethrow:
			case Code.Sizeof:
			case Code.Refanytype:
			case Code.Readonly:
				break;
			default:
				throw new OptimizerAssertionException ();
			}
		}
	}
}
