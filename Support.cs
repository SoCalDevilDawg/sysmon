using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

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
