//
// Extensions.cs
//
// Author:
//       M.A. (https://github.com/mkahvi)
//
// Copyright (c) 2017–2019 M.A.
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

using System.Diagnostics;

namespace SystemMonitor
{
	static public class Extensions
	{
		public static float LimitRange(this float value, float inclusiveMinimum, float inclusiveMaximum)
		{
			return (value < inclusiveMinimum) ? inclusiveMinimum : ((value > inclusiveMaximum) ? inclusiveMaximum : value);
		}

		public static int ToSimpleInt(this ProcessPriorityClass value)
			=> value switch
			{
				ProcessPriorityClass.Idle => 0,
				ProcessPriorityClass.BelowNormal => 1,
				ProcessPriorityClass.AboveNormal => 3,
				ProcessPriorityClass.High => 4,
				_ => 2,
			};
	}

	public static class ProcessPriority
	{
		public static ProcessPriorityClass FromInt(int value)
			=> value switch
			{
				0 => ProcessPriorityClass.Idle,
				1 => ProcessPriorityClass.BelowNormal,
				3 => ProcessPriorityClass.AboveNormal,
				4 => ProcessPriorityClass.High,
				_ => ProcessPriorityClass.Normal,
			};

	}
}
