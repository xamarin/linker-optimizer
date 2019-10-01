//
// MonoLinkerSupport.cs
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
namespace System.Runtime.CompilerServices
{
#if !INSIDE_CORLIB
	public
#endif
	static class MonoLinkerSupport
	{
		[MethodImpl(MethodImplOptions.NoInlining)]
		public static bool IsWeakInstanceOf<T> (object obj) => obj is T;

		[MethodImpl(MethodImplOptions.NoInlining)]
		public static bool IsTypeAvailable<T> () => true;

		// This should only be used in tests.
		[MethodImpl(MethodImplOptions.NoInlining)]
		public static bool IsTypeAvailable (string type) => true;

		[MethodImpl(MethodImplOptions.NoInlining)]
		public static bool AsWeakInstanceOf<T> (object obj, out T instance) where T : class
		{
			instance = obj as T;
			return instance != null;
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		public static bool IsFeatureSupported (MonoLinkerFeature feature) => true;

		[MethodImpl(MethodImplOptions.NoInlining)]
		public static void RequireFeature (MonoLinkerFeature feature)
		{ }
	}
}
