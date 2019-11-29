//
// OHW.cs
//
// Author:
//       M.A. (https://github.com/mkahvi)
//
// Copyright (c) 2019 M.A.
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

namespace SystemMonitor
{
	public class OHW : IDisposable
	{
		public OHW()
		{
			OpenHardwareMonitor.Hardware.Computer computer;

			computer = new OpenHardwareMonitor.Hardware.Computer()
			{
				GPUEnabled = true,
				CPUEnabled = true,
				//MainboardEnabled = true, // doesn't seem to have anything
				FanControllerEnabled = true,
			};

			computer.Open();
			if (computer.Hardware.Length == 0)
			{
				computer.Close();
				throw new Exception("OHM failed to initialize.");
			}

			try
			{
				foreach (var hw in computer.Hardware)
				{
					hw.Update();
					/*
					foreach (var shw in hw.SubHardware)
						shw.Update();
					*/

					/*
					 *
					 * CPU Core #1 : Load = 60.00
					 * CPU Core #2 : Load = 40.00
					 * CPU Total : Load = 50.00
					 * CPU Core #1 : Clock = 3,411.70
					 * CPU Core #2 : Clock = 3,411.70
					 *
					 * GPU Core : Temperature = 42.00
					 * GPU : Fan = 1,110.00
					 * GPU Core : Clock = 796.94
					 * GPU Memory : Clock = 3,004.68
					 * GPU Shader : Clock = 1,593.87
					 * GPU Core : Load = 3.00
					 * GPU Memory Controller : Load = 2.00
					 * GPU Video Engine : Load = 0.00 // this is "always" zero, useless stat
					 * GPU Fan : Control = 32.00
					 * GPU Memory Total : SmallData = 2,048.00
					 * GPU Memory Used : SmallData = 1,301.16
					 * GPU Memory Free : SmallData = 746.84
					 * GPU Memory : Load = 63.53
					 */

					Console.WriteLine("Hardware: " + hw.Name + $" ({hw.HardwareType.ToString()})");

					switch (hw.HardwareType)
					{
						case OpenHardwareMonitor.Hardware.HardwareType.CPU:
							// only load and clock sensors normally
							// temperature sensor requires admin rights
							foreach (var sensor in hw.Sensors)
							{
								if (sensor.SensorType == OpenHardwareMonitor.Hardware.SensorType.Temperature)
									cpuTemp = sensor;
							}
							break;
						case OpenHardwareMonitor.Hardware.HardwareType.GpuAti:
						case OpenHardwareMonitor.Hardware.HardwareType.GpuNvidia:
							GPUName = hw.Name;
							gpu = hw;
							foreach (var sensor in hw.Sensors)
							{
								switch (sensor.Name)
								{
									default: break; // ignore
									case "GPU":
										if (sensor.SensorType == OpenHardwareMonitor.Hardware.SensorType.Fan)
											gpuFan = sensor;
										break;
									case "GPU Core":
										switch (sensor.SensorType)
										{
											case OpenHardwareMonitor.Hardware.SensorType.Temperature:
												gpuTmp = sensor;
												break;
											case OpenHardwareMonitor.Hardware.SensorType.Load:
												gpuLoad = sensor;
												break;
											case OpenHardwareMonitor.Hardware.SensorType.Clock:
												gpuClock = sensor;
												break;
										}
										break;
									case "GPU Fan":
										if (sensor.SensorType == OpenHardwareMonitor.Hardware.SensorType.Control)
											gpuFanControl = sensor;
										break;
									case "GPU Memory":
										if (sensor.SensorType == OpenHardwareMonitor.Hardware.SensorType.Load)
											gpuMemLoad = sensor;
										break;
									case "GPU Memory Total":
										if (sensor.SensorType == OpenHardwareMonitor.Hardware.SensorType.SmallData)
											GPUTotalMemory = sensor.Value ?? float.NaN;
										break;
									case "GPU Memory Controller":
										if (sensor.SensorType == OpenHardwareMonitor.Hardware.SensorType.Load)
											gpuMemCtrl = sensor;
										break;
								}
							}
							break;
					}
				}

				Initialized = true;
			}
			catch (Exception ex)
			{
				throw;
			}
			finally
			{
				computer?.Close(); // not needed?
			}
		}

		public void Update() => gpu?.Update();

		/// <summary>
		/// Total VRAM in MB.
		/// </summary>
		public float GPUTotalMemory { get; private set; } = float.NaN;

		/// <summary>
		/// 100f to 0f % memory usage.
		/// </summary>
		public float GPUMemoryLoad => gpuMemLoad?.Value ?? float.NaN;

		public float GPUMemCtrlLoad => gpuMemCtrl?.Value ?? float.NaN;

		/// <summary>
		/// Fan RPM
		/// </summary>
		public float GPUFanSpeed => gpuFan?.Value ?? float.NaN;

		/// <summary>
		/// 100f to 0f % fan load.
		/// </summary>
		public float GPUFanLoad => gpuFanControl?.Value ?? float.NaN;

		public float GPUClock => gpuClock?.Value ?? float.NaN;

		public float GPUTemperature => gpuTmp?.Value ?? float.NaN;

		public float CPUTemperature => cpuTemp?.Value ?? float.NaN;

		/// <summary>
		/// 100f to 0f % GPU load.
		/// </summary>
		public float GPULoad => gpuLoad?.Value ?? float.NaN;

		readonly bool Initialized = false;

		public string GPUName { get; private set; } = string.Empty;

		OpenHardwareMonitor.Hardware.IHardware? gpu = null;
		OpenHardwareMonitor.Hardware.ISensor?
			gpuFan = null, // Fan speed
			gpuFanControl = null, // Fan speed controller. Value is % usage
			gpuTmp = null, // Temperature
			gpuMemLoad = null, // Free Memory
			gpuClock = null, // Core clock speed
			gpuLoad = null, // Core % load
			gpuMemCtrl = null, // Memory Controller
			cpuTemp = null; // CPU temperature

		#region IDisposable Support
		private bool Disposed = false;

		protected virtual void Dispose(bool disposing)
		{
			if (!Disposed)
			{
				if (disposing)
				{
					// TODO: dispose managed state (managed objects).
				}

				Disposed = true;
			}
		}

		~OHW() => Dispose(false);

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		#endregion
	}
}
