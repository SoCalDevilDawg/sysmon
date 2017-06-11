//
// PerformanceCounterWrapper.cs
//
// Author:
//       M.A. (enmoku) <>
//
// Copyright (c) 2017 M.A. (enmoku)
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
using System.Collections.Generic;
using System.Diagnostics;

namespace SystemMonitor
{
	public class PerformanceCounterWrapper : IDisposable
	{
		public static List<PerformanceCounter> Sensors = new List<PerformanceCounter>(3);

		public PerformanceCounter Counter { get; private set; }

		public PerformanceCounterWrapper(string category, string counter, string instance = null, bool scrapfirst = true)
		{
			Counter = new System.Diagnostics.PerformanceCounter()
			{
				CategoryName = category,
				CounterName = counter,
				InstanceName = instance,
				ReadOnly = true,
			};

			Sensors.Add(Counter);

			if (scrapfirst)
			{
				var scrap = Value;
			}
		}

		bool disposed = false;

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (disposed) return;
			if (disposing)
			{
				Counter.Close();
				Counter.Dispose();
				Counter = null;
				disposed = true;
			}
		}

		public float Value
		{
			get
			{
				try
				{
					return Counter.NextValue();
				}
				catch (System.InvalidOperationException)
				{
					// TODO: Driver/Adapter vanished and other problems, try to re-acquire it.
					//Console.WriteLine("DEBUG :: PFC(" + _pfc.CategoryName + "//" + _pfc.CounterName + "//" + _pfc.InstanceName + ") vanished.");
				}
				return float.NaN;
			}
		}

		public CounterSample Sample
		{
			get
			{
				return Counter.NextSample();
			}
		}
	}
}
