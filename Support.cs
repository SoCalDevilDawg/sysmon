//
// Support.cs
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

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace SystemMonitor
{
	public class Support
	{
		public Support() => Idle();

		readonly Stopwatch stopwatch = Stopwatch.StartNew();
		long oldIdleMs = 0, oldUsedMs;

		public float UsageFromIdle(float idle) => (1f - idle) * 100f;

		readonly int CoreCount = Environment.ProcessorCount;

		public float Idle()
		{

			var period = stopwatch.ElapsedMilliseconds;
			GetSystemTimes(out var idle, out var kernel, out var user);
			stopwatch.Restart();
			long newIdleMs = FiletimeToLong(idle);
			//long newUsedMs = (FiletimeToLong(user) + FiletimeToLong(kernel)); // unused

			//var usedMs = (newUsedMs - oldUsedMs) / (10_000 * Environment.ProcessorCount); // unused
			var idleMs = (newIdleMs - oldIdleMs) / (10_000 * CoreCount);
			oldIdleMs = newIdleMs;
			//oldUsedMs = newUsedMs; // unused

			//Logging.DebugMsg("CPU --- idle: " + idleMs.ToString() + " --- used: " + usedMs.ToString() + " --- " + period.ToString() + " --- % = " + ((float)idleMs / period).ToString("N1"));

			return (float)idleMs / period;
		}

		[DllImport("kernel32.dll", SetLastError = true)]
		static extern bool GetSystemTimes(out System.Runtime.InteropServices.ComTypes.FILETIME lpIdleTime, out System.Runtime.InteropServices.ComTypes.FILETIME lpKernelTime, out System.Runtime.InteropServices.ComTypes.FILETIME lpUserTime);

		static long FiletimeToLong(System.Runtime.InteropServices.ComTypes.FILETIME ft) => ((long)ft.dwHighDateTime << 32) | (uint)ft.dwLowDateTime;
	}
}
