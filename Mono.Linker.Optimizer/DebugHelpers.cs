//
// DebugHelpers.cs
//
// Author:
//       Martin Baulig <mabaul@microsoft.com>
//
// Copyright (c) 2019 Microsoft Corporation
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Runtime.CompilerServices;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Mono.Linker.Optimizer
{
	using BasicBlocks;

	static class DebugHelpers
	{
		public static Exception AssertFail (string message) => throw new MartinAssertException (message);

		public static Exception AssertFail (MethodDefinition method, string message , [CallerMemberName] string caller = null)
		{
			throw new MartinAssertException ($"Assertion failed in `{method}`: {message}{(!string.IsNullOrEmpty (caller) ? " (at " + caller + ")" : "")}");
		}

		public static Exception AssertFail (MethodDefinition method, BasicBlock block, string message, [CallerMemberName] string caller = null)
		{
			throw new MartinAssertException ($"Assertion failed in `{method}` ({block}): {message}{(!string.IsNullOrEmpty (caller) ? " (at " + caller + ")" : "")}");
		}

		public static Exception AssertFailUnexpected (MethodDefinition method, BasicBlock block, object unexpected, [CallerMemberName] string caller = null)
		{
			string message;
			if (unexpected == null)
				message = "got unexpected null reference";
			else if (unexpected is Instruction instruction)
				message = $"got unexpected instruction `{CecilHelper.Format (instruction)}`";
			else
				message = $"got unexpected `{unexpected}`";

			throw AssertFail (method, block, message, caller);
		}

		public static void Assert (bool condition, [CallerMemberName] string caller = null)
		{
			if (!condition)
				throw new MartinAssertException ($"Assertion failed{(!string.IsNullOrEmpty (caller) ? " in " + caller : "")}.");
		}
	}
}
