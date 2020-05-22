﻿//
// JumpOrigin.cs
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
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Mono.Linker.Optimizer.BasicBlocks
{
	public class JumpOrigin
	{
		public BasicBlock Target {
			get; set;
		}

		public BasicBlock OriginBlock {
			get; set;
		}

		public Instruction Origin {
			get;
		}

		public ExceptionHandler Exception {
			get;
		}

		public JumpOrigin (BasicBlock target, BasicBlock current, Instruction origin)
		{
			Target = target;
			Origin = origin;
			OriginBlock = current;
		}

		public JumpOrigin (BasicBlock target, ExceptionHandler handler)
		{
			Target = target;
			Exception = handler;
		}

		public override int GetHashCode () => base.GetHashCode ();

		public override bool Equals (object obj)
		{
			if (obj is JumpOrigin other) {
				if (Target != other.Target)
					return false;
				if (Exception != null)
					return other.Exception == Exception;
				if (other.Exception != null)
					return false;
				return Origin == other.Origin;
			}
			return false;
		}

		public override string ToString ()
		{
			if (Exception != null)
				return $"[{GetType ().Name}: {Target} {Exception.HandlerType}]";
			return $"[{GetType ().Name}: {Target} <== {OriginBlock} - {CecilHelper.Format (Origin)}]";
		}
	}
}
