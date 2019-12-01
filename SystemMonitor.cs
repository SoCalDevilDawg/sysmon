//
// SystemMonitor.cs
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

using MKAh;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using static MKAh.CoreTypeExtensions;

namespace SystemMonitor
{
	public class ProcessPFC : IDisposable
	{
		public string Name { get; set; }

		public PerformanceCounterWrapper CPU { get; set; }
		public PerformanceCounterWrapper MEM { get; set; }
		public PerformanceCounterWrapper NVM { get; set; }
		//public PerformanceCounterWrapper NET { get; set; } // Impossible?

		public ProcessPFC(string process)
		{
			Name = process;
			CPU = new PerformanceCounterWrapper("Process", "% Processor Time", Name);
			MEM = new PerformanceCounterWrapper("Process", "Private Bytes", Name);
			NVM = new PerformanceCounterWrapper("PhysicalDisk", "Disk Bytes/sec", Name);
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
				CPU.Dispose();
				MEM.Dispose();
				NVM.Dispose();
				CPU = null;
				MEM = null;
				NVM = null;

				disposed = true;
			}
		}
	}

	public class MainWindow : Form
	{
		protected override CreateParams CreateParams
		{
			get
			{
				CreateParams cp = base.CreateParams;
				cp.ExStyle |= 0x02000000; //WS_EX_COMPOSITED. Prevents flickering.
				return cp;
			}
		}

		float bottleneck_mem = 0, bottleneck_cpu = 0, bottleneck_nvm = 0;

		float LowMemThreshold = 4;
		float LowMemMultiplier = 5.2f;

		string[] IgnoreProcesses = { "svchost", "wininit", "System", "Idle" };

		Dictionary<string, ProcessPFC> Processes = new Dictionary<string, ProcessPFC>(3);

		int i = 0;

		async System.Threading.Tasks.Task CheckLoaders()
		{
			//if (i++ < 5) return;
			i = 0;

			//await System.Threading.Tasks.Task.Delay(100).ConfigureAwait(false);

			var procs = System.Diagnostics.Process.GetProcesses();
			Process memloader = null;
			long memloadersize = 0;
			Process nvmloader = null;
			float nvmloadersize = 0;
			Process cpuloader = null;
			float cpuloadersize = 0;

			/*
			PerformanceCounterCategory processCategory = new PerformanceCounterCategory("Processor");
			var names = processCategory.GetInstanceNames(); // _total, 0, 1, 2, 3 (cores?)
			foreach (var name in names)
				Console.WriteLine(name);
			*/

			foreach (var proc in procs)
			{
				string name = proc.ProcessName;
				if (IgnoreProcesses.Contains(name)) continue;

				ProcessPFC current = null;
				if (!Processes.ContainsKey(name))
					Processes.Add(name, current = new ProcessPFC(name));
				else
					current = Processes[name];

				float cpuload = current.CPU.Value;
				float memload = current.MEM.Value;
				float nvmload = current.NVM.Value;
				if (float.IsNaN(cpuload) || float.IsNaN(memload) || float.IsNaN(nvmload))
				{
					Processes.Remove(current.Name);
					current.Dispose();
					continue;
				}
				//Console.WriteLine("ProcessPFC({0}) has {1} processes. Load: {2:N2}", name, current.Processes.Count, cpuload);

				if (proc.PrivateMemorySize64 > memloadersize)
				{
					memloader = proc;
					memloadersize = proc.PrivateMemorySize64;
				}
				if (cpuload > cpuloadersize)
				{
					cpuloader = proc;
					cpuloadersize = cpuload;
				}
				if (nvmload > cpuloadersize)
				{
					nvmloader = proc;
					nvmloadersize = nvmload;
				}
			}

			//Console.WriteLine("ProcessPFC count: {0}", Processes.Count);

			if (memloader != null)
				Console.WriteLine("Mem loader: {0} ({1:N2} MiB)", memloader.ProcessName, memloader.PrivateMemorySize64 / 1024000);
			if (cpuloader != null)
				Console.WriteLine("CPU loader: {0} ({1:N2}%)", cpuloader.ProcessName, cpuloadersize);
			if (nvmloader != null)
				Console.WriteLine("NVM loader: {0} ({1:N2} kB/s)", nvmloader.ProcessName, nvmloadersize / 1000);
			// PrivateMemorySize64
		}

		Support support = new Support();

		void UpdateSensors()
		{
			//var cpuusaget = cpuusage.Value;
			var cpuusaget = support.UsageFromIdle(support.Idle());

			/*
			string[] coreusaget = new string[cores];
			for (int i = 0; i < cores; i++)
			{
				coreusaget[i] = string.Format("{0:N0}", coreusage[i].Value);
			}
			var coreusagett = string.Join("/", coreusaget);
			*/
			var cpuqueuet = cpuqueue.Value;
			var interruptt = interrupt.Value;

			/*
			if (float.IsNaN(cpuusaget) || float.IsNaN(cpuqueuet) || float.IsNaN(interruptt))
			{
				// TODO: Handle this
			}
			*/

			//Sensor_CPU.Value.Text = string.Format("{0:N1}%\n{1}\n{2} queued\n{3:N1}% interrupt", cpuusaget, coreusagett, cpuqueuet, interruptt);

			if (AdminSensors)
				Sensor_CPU.Value.Text = string.Format("{0:N1}%\n{1:N0} C\n{2} queued\n{3:N1}% interrupt", cpuusaget, HWmon.CPUTemperature, cpuqueuet, interruptt);
			else
				Sensor_CPU.Value.Text = string.Format("{0:N1}%\n{1} queued\n{2:N1}% interrupt", cpuusaget, cpuqueuet, interruptt);

			// BOTTLENECK :: CPU
			float cpucurve = (Convert.ToSingle(Math.Pow(((cpuusaget / 100f) + 0.4f), 7.1f)) - ((cpuusaget / 100f) / 10f)).LimitRange(0f, 10f);
			bottleneck_cpu = cpucurve + (cpuqueuet / 2) + (interruptt / 4);
			Sensor_CPU.Chart.Add(cpuusaget);

			UpdateMemory(out var memfreetb, out var memtotaltb);

			const float bytesToGBDivisor = 1024f * 1024f * 1024f;

			//float memusedt = (memtotaltb - memfreetb) / bytesToGBDivisor; // used mem
			float memfreet = memfreetb / bytesToGBDivisor; // free mem in GB

			// Actual as reported, and memory as determined programmatically. The difference is likely disk caching space.
			// Memory pressure going over the "true" value is likely going to result in poor performance.
			float privmem = privatememory.Value;

			//float memfreet = memfree.Value / 1024; // free memory in GB
			float mempressure = (privmem / TotalMemory);
			//float memfreert = (TotalMemory - privmem) / 1024000000;

			Sensor_Memory.Value.Text = $"{memfreet:N2} GiB free\n{memcommit.Value:N1}% commit\n{mempressure * 100f:N1}% pressure";

			var tempmem = (TotalMemoryMB) - (memfreet * 1000);

			Sensor_Memory.Chart.Add(tempmem);

			// BOTTLENECK :: MEM
			bottleneck_mem =
				(mempressure * 10f) // 1 to 0 memory pressure into 10 to 0 scale.
				+ ((LowMemThreshold - memfreet) * LowMemMultiplier).Min(0f); // low memory

			var splitiot = splitio.Value;
			var highavgt = Math.Max(avgnvmread.Value * 1000, avgnvmwrite.Value * 1000);
			var nvmqueuet = nvmqueue.Value;
			var nvmtimet = nvmtime.Value;
			var nvmbytesr = nvmbytes.Value;
			var nvmbytest = nvmbytesr / 1000000;
			var nvmtransferst = nvmtransfers.Value;
			Sensor_NVMIO.Value.Text = $"{nvmbytest:N2} MB\n{nvmtransferst:N1} transfers\n{highavgt:N1}ms delay\n{splitiot:N2} splits\n{nvmqueuet} queued";
			Sensor_NVMIO.Chart.Add(nvmbytest);

			// Disk time >20%
			// Split I/O count >= 2

			// BOTTLENECK :: NVM/HDD/SSD
			bottleneck_nvm = nvmqueuet.LimitRange(0, 8) + (splitiot * 0.3f).LimitRange(0, 4) + (highavgt * 0.08f) + (nvmtransferst / 500).LimitRange(0, 1);

			// PAGE FAULTS
			var faultcount = pagefault.Value;
			var pagereadcount = pagereads.Value;
			if (float.IsNaN(faultcount) || float.IsNaN(pagereadcount))
			{
				// TODO: Handle error
				Sensor_PageFault.Value.Text = "ERROR";
				Sensor_PageFault.Chart.Add(0);
			}
			else
			{
				Sensor_PageFault.Value.Text = string.Format("{0:N1} reads/sec\n{1:N2}% hard faults\n{2:N2}% NVM use",
															pagereadcount, (faultcount <= 0.0001 ? 0 : (pagereadcount / faultcount)), (nvmbytesr <= 0.0001 ? 0 : ((pagereadcount * 4048) / nvmbytesr)));
				//Console.WriteLine("Page Faults = " + pf);
				//Console.WriteLine("Page Reads  = " + pr);
				//Console.WriteLine("Page Fault % = " + (pf / pr));
				Sensor_PageFault.Chart.Add(faultcount <= 0.0001 ? 0 : (pagereadcount / faultcount));
			}

			// NETWORK
			var netint = netin.Value / 1000;
			var netoutt = netout.Value / 1000;
			Sensor_NetIO.Value.Text = string.Format("{0:N1} kB In\n{1:N1} kB Out\n{2} queued", netint, netoutt, netqueue.Value);
			double curnetio = netoutt + netint;
			Sensor_NetIO.Chart.Add(curnetio);

			// GPU
			float bottleneck_gpu = float.MinValue, gpuload = float.MinValue, gpumem = float.MinValue;
			if (OHWSensors)
			{
				UpdateGPU(out gpuload, out gpumem);
				bottleneck_gpu = gpuload + gpumem;
			}

			// TODO: Make this more dynamic
			if (bottleneck_mem >= 8)
				Sensor_Bottleneck.Value.Text = "MEM";
			else if (bottleneck_nvm >= 8)
				Sensor_Bottleneck.Value.Text = "NVM";
			else if (bottleneck_cpu >= 8)
				Sensor_Bottleneck.Value.Text = "CPU";
			else if (bottleneck_gpu >= 8)
				Sensor_Bottleneck.Value.Text = "GPU";
			else
				Sensor_Bottleneck.Value.Text = "---";

			if (OHWSensors)
				Sensor_Bottleneck.Value.Text += string.Format("\nMEM: {0:N1}\nNVM: {1:N1}\nCPU: {2:N1}\nGPU: {3:N1}", bottleneck_mem, bottleneck_nvm, bottleneck_cpu, bottleneck_gpu);
			else
				Sensor_Bottleneck.Value.Text += string.Format("\nMEM: {0:N1}\nNVM: {1:N1}\nCPU: {2:N1}", bottleneck_mem, bottleneck_nvm, bottleneck_cpu);

			if (FlashWarnings)
			{
				if (cpuusaget >= 85.0 || cpuqueuet >= 5.0) Sensor_CPU.Warn();

				if ((memfreet <= Settings.Current.FreeMemoryThreshold) || (mempressure >= (Settings.Current.MemoryPressureThreshold))) Sensor_Memory.Warn();

				if (splitiot >= 2 || highavgt >= 45 || nvmtimet >= 20) Sensor_NVMIO.Warn();

				if (Math.Max(Math.Max(bottleneck_cpu, bottleneck_nvm), bottleneck_mem) >= 6) Sensor_Bottleneck.Warn();

				if (OHWSensors)
				{
					if (gpuload >= 8.5f) Sensor_GPU_Load.Warn();
					if (gpumem >= 8.5f) Sensor_GPU_MEM.Warn();
				}
			}

			// CheckLoaders();
		}

		//PerformanceCounterWrapper cpuusage; // unused
		//PerformanceCounterWrapper[] coreusage;
		PerformanceCounterWrapper cpuqueue;
		PerformanceCounterWrapper interrupt;

		//PerformanceCounterWrapper memfree; // unused
		PerformanceCounterWrapper memcommit;
		PerformanceCounterWrapper privatememory;

		PerformanceCounterWrapper nvmbytes;
		PerformanceCounterWrapper nvmtime;
		PerformanceCounterWrapper nvmqueue;
		PerformanceCounterWrapper nvmtransfers;
		PerformanceCounterWrapper splitio;
		PerformanceCounterWrapper avgnvmread;
		PerformanceCounterWrapper avgnvmwrite;

		PerformanceCounterWrapper netin;
		PerformanceCounterWrapper netout;
		PerformanceCounterWrapper netqueue;

		PerformanceCounterWrapper pagefault;
		PerformanceCounterWrapper pagereads;
		//PerformanceCounterWrapper
		// DPC : Deferred Procedure Call - delay
		// ISR : Interrupt Service Routine - delay
		// Kernel latency

		OHW? HWmon = null;

		int cores;

		void InitCPUUsageCounters()
		{
			var procDict = new Dictionary<string, float>();
			var counterList = new List<PerformanceCounter>();
			Process.GetProcesses().ToList().ForEach(p =>
			{
				using (p)
				{
					if (p.Id <= 4) return;
					if (counterList.FirstOrDefault(c => c.InstanceName == p.ProcessName) == null)
					{
						var counter = new PerformanceCounter("Process", "% Processor Time", p.ProcessName, true);
						counter.NextValue();
						counterList.Add(counter);
					}
				}
			});
			counterList.ForEach(c =>
			{
				try
				{
					var percent = c.NextValue() / Environment.ProcessorCount;
					if (Math.Abs(percent) < double.Epsilon)
						return;

					procDict[c.InstanceName] = percent;
				}
				catch (InvalidOperationException) { /* some will fail */ }
			});
		}

		void InitCounters()
		{
			Console.WriteLine("Initializing counters...");

			//cpuusage = new PerformanceCounterWrapper("Processor", "% Processor Time", "_Total");

			/*
			var cpus = new PerformanceCounterCategory("Processor").GetInstanceNames();
			cores = cpus.Count() - 1;
			Console.WriteLine("Cores: {0}", cores);
			coreusage = new PerformanceCounterWrapper[cores];
			for (int core = 0; core < cores; core++)
				coreusage[core] = new PerformanceCounterWrapper("Processor", "% Processor Time", core.ToString());
			*/

			//new PerformanceCounterWrapper("Processor", "% Processor Time", "processnamewithoutexe");
			//new PerformanceCounterWrapper("Process", "Private Bytes", "processnamewithoutexe");
			//new PerformanceCounterWrapper("PhysicalDisk", "Disk Bytes/sec", "processnamewithoutexe");

			//memfree = new PerformanceCounterWrapper("Memory", "Available MBytes", null);
			//var memfree = new PerformanceCounterWrapper("Memory", "Available Bytes", null);

			memcommit = new PerformanceCounterWrapper("Memory", "% Committed Bytes In Use", null); // swap usage?
			privatememory = new PerformanceCounterWrapper("Process", "Private Bytes", "_Total"); // memory pressure in bytes

			cpuqueue = new PerformanceCounterWrapper("System", "Processor Queue Length", null); // > 5 bad

			nvmbytes = new PerformanceCounterWrapper("PhysicalDisk", "Disk Bytes/sec", "_Total");
			nvmtime = new PerformanceCounterWrapper("PhysicalDisk", "% Disk Time", "_Total");
			nvmqueue = new PerformanceCounterWrapper("PhysicalDisk", "Current Disk Queue Length", "_Total"); // > 5 bad

			var nic = new PerformanceCounterCategory("Network Interface").GetInstanceNames()[1]; // 0 = loopback
			netin = new PerformanceCounterWrapper("Network Interface", "Bytes Received/sec", nic);
			netout = new PerformanceCounterWrapper("Network Interface", "Bytes Sent/sec", nic);
			netqueue = new PerformanceCounterWrapper("Network Interface", "Output Queue Length", nic);

			splitio = new PerformanceCounterWrapper("LogicalDisk", "Split IO/sec", "_Total");
			nvmtransfers = new PerformanceCounterWrapper("LogicalDisk", "Disk Transfers/sec", "_Total");
			avgnvmread = new PerformanceCounterWrapper("LogicalDisk", "Avg. Disk Sec/Read", "_Total");
			avgnvmwrite = new PerformanceCounterWrapper("LogicalDisk", "Avg. Disk Sec/Write", "_Total");

			interrupt = new PerformanceCounterWrapper("Processor", "% Interrupt Time", "_Total");

			pagefault = new PerformanceCounterWrapper("Memory", "Page Faults/sec", null);
			pagereads = new PerformanceCounterWrapper("Memory", "Page Reads/sec", null);

			/*
			// Unreliable if router is involved.
			var netband = new PerformanceCounterWrapper("Network Interface", "Current Bandwidth", nic);
			Console.WriteLine("Bandwidth: {0:N0} kB", netband.Value/1000);
			*/

			if (OHWSensors)
			{
				HWmon = new OHW();
				AdminSensors = MKAh.Execution.IsAdministrator;
			}

			Console.WriteLine("Initialization complete.");
		}

		ulong TotalMemory = 0, TotalMemoryMB = 0;

		SensorChunk Sensor_Bottleneck, Sensor_CPU, Sensor_Memory, Sensor_PageFault, Sensor_NVMIO, Sensor_NetIO, Sensor_GPU_MEM, Sensor_GPU_Load;

		bool OHWSensors = false;
		bool AdminSensors = false;

		bool disposed = false;

		protected override void Dispose(bool disposing)
		{
			if (disposed) return;

			if (disposing)
			{
				base.Dispose(disposing);
				disposed = true;
			}
		}

		readonly MenuItem
			lowpriocmo, normpriocmo, highpriocmo, togglewarn,
			updateFreq05, updateFreq15, updateFreq25;

		bool FlashWarnings = true;

		bool RunAtStart(bool status, bool dryrun = false)
		{
			string runatstart_path = @"Software\Microsoft\Windows\CurrentVersion\Run";
			string runatstart_key = "MKAh-SystemMonitor";
			string runatstart;
			Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(runatstart_path, true);
			if (key != null)
			{
				runatstart = (string)key.GetValue(runatstart_key, string.Empty);
				if (dryrun) return (runatstart == Environment.GetCommandLineArgs()[0]);
				if (status)
				{
					if (runatstart == Environment.GetCommandLineArgs()[0]) return true;
					key.SetValue(runatstart_key, Environment.GetCommandLineArgs()[0]);
					Console.WriteLine("Startup enabled: " + Environment.GetCommandLineArgs()[0]);
					return true;
				}
				else if (!status)
				{
					if (runatstart != Environment.GetCommandLineArgs()[0]) return false;

					key.DeleteValue(runatstart_key);
					Console.WriteLine("Run at Startup disabled.");
					//return false;
				}
			}
			return false;
		}

		System.Windows.Forms.Timer UITimer;

		public MainWindow(bool ohwsensors)
		{
			OHWSensors = ohwsensors;

			Text = "System Monitor";
			WindowState = FormWindowState.Normal;
			FormBorderStyle = FormBorderStyle.SizableToolWindow; // no min/max buttons as wanted
			SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.OptimizedDoubleBuffer, true);
			MinimizeBox = false;
			MaximizeBox = false;
			// monitors = 6
			MinimumSize = new Size(650, 136);
			AutoSizeMode = AutoSizeMode.GrowOnly;
			AutoSize = true;
			Size = new System.Drawing.Size(650, 136);
			Padding = new Padding(3);

			BackColor = Settings.Current.BackColor;

			Console.WriteLine("Analyzing system...");
			try
			{
				using (var memsearch = new ManagementObjectSearcher(@"root\CIMV2", "SELECT TotalPhysicalMemory FROM Win32_ComputerSystem"))
				{
					foreach (ManagementObject queryObj in memsearch.Get())
					{
						TotalMemory = (ulong)(queryObj["TotalPhysicalMemory"] ?? 0);
						TotalMemoryMB = TotalMemory / 1024000;
						break;
					}
				}
			}
			catch (System.Runtime.InteropServices.COMException e)
			{
				MessageBox.Show("A COM error occurred while querying for WMI data: " + e.Message);
				return;
			}
			catch (ManagementException e)
			{
				MessageBox.Show("An error occurred while querying for WMI data: " + e.Message);
				return;
			}

			// READ CONFIGURATION

			Console.WriteLine("Configuration start.");

			Console.WriteLine($"+ Free mem threshold: {Settings.Current.FreeMemoryThreshold:N2}");
			Console.WriteLine($"+ Memory pressure:    {Settings.Current.MemoryPressureThreshold:N2}");

			/*
			string positionkey = "Position";
			//if (settings[positionkey] == null) settings.Add(positionkey, Location.X + "," + Location.Y);
			var coords = settings.Get(positionkey)?.Split(',');
			if (coords != null)
			{
				var x = Convert.ToInt32(coords?[0]);
				var y = Convert.ToInt32(coords?[1]);
				PointToScreen(new Point(x, y));
				Console.WriteLine("+ Position:           {0},{1}", x, y);
			}
			*/
			Console.WriteLine("Configuration end.");

			/*
			FormClosing += (sender, e) => {
				try
				{
					if (settings[positionkey] == null) settings.Add(positionkey, Location.X + "," + Location.Y);
					Console.WriteLine("+ Position: {0},{1}", Location.X, Location.Y);
				}
				catch
				{
					Console.WriteLine("Failed to write settings.");
				}
			};
			*/

			// INITIALIZE
			InitCounters();

			var tooltip = new ToolTip();

			// https://msdn.microsoft.com/en-us/library/system.io.filesystemwatcher(v=vs.110).aspx

			var layout = new TableLayoutPanel
			{
				Parent = this,
				//ColumnCount = 8,
				RowCount = 1,
				Dock = DockStyle.Left,
				GrowStyle = TableLayoutPanelGrowStyle.AddColumns,
				AutoSize = true,
			};

			// Fill in with whatever resource is getting getting run out of


			//var bottlenecklabel = new SensorHeader { Text = "Bottleneck" };
			//bottleneckvalue = new SensorValue();

			Sensor_Bottleneck = new SensorChunk("Bottleneck");
			tooltip.SetToolTip(Sensor_Bottleneck, "The values are arbitrary but usually 6+ is moderate load while 10+ should mean heavy load.");

			Sensor_CPU = new SensorChunk("CPU", chart: true);
			Sensor_CPU.Chart.MaxValue = 100.0d; // 100%
			Sensor_CPU.Chart.StaticRange = true;
			tooltip.SetToolTip(Sensor_CPU.Value, "Queued command counter is clearest indicator of underscaled CPU.\nInterrupt percentage shows load from peripherals, NICs, extension cards, and such.");

			Sensor_Memory = new SensorChunk("Memory", chart: true, horizontal: true);
			tooltip.SetToolTip(Sensor_Memory.Value, "Physical memory usage.\nCommit is swap file usage.\nPressure is private memory load.");
			Sensor_Memory.Chart.MaxValue = TotalMemoryMB;

			if (OHWSensors)
			{
				Sensor_GPU_MEM = new SensorChunk("GPU MEM", chart: true, horizontal: true);
				Sensor_GPU_MEM.Chart.MaxValue = 100.0d;
				Sensor_GPU_MEM.Chart.StaticRange = true;

				Sensor_GPU_Load = new SensorChunk("GPU Load", chart: true);
				Sensor_GPU_Load.Chart.MaxValue = 100.0d;
				Sensor_GPU_Load.Chart.StaticRange = true;
			}

			Sensor_PageFault = new SensorChunk("Page Faults", chart: true);
			tooltip.SetToolTip(Sensor_PageFault.Value, "Page file performance degradation.\nPage faults themselves are not to worry.\nHard page faults can be source of poor performance.");

			Sensor_NVMIO = new SensorChunk("NVM", chart: true);
			tooltip.SetToolTip(Sensor_NVMIO.Header, "Non-volatile memory: HDD, SSD, etc.\nThese are not clear indicators of bottlenecks if multiple NVMs are involved.");
			tooltip.SetToolTip(Sensor_NVMIO.Value, "Split indicates fragmentation performance loss.\nQueue&delay indicates slow NVM.");

			Sensor_NetIO = new SensorChunk("Network", chart: true);
			tooltip.SetToolTip(Sensor_NetIO.Value, "Queue length is indicator of too slow or bad outbound connection.");

			layout.Controls.Add(Sensor_Bottleneck);
			layout.Controls.Add(Sensor_CPU);
			layout.Controls.Add(Sensor_Memory);
			if (OHWSensors)
			{
				layout.Controls.Add(Sensor_GPU_Load);
				layout.Controls.Add(Sensor_GPU_MEM);
			}
			layout.Controls.Add(Sensor_PageFault);
			layout.Controls.Add(Sensor_NVMIO);
			layout.Controls.Add(Sensor_NetIO);

			ContextMenu = new ContextMenu();
			togglewarn = ContextMenu.MenuItems.Add("Warnings", (sender, e) =>
			{
				FlashWarnings = !FlashWarnings;
				togglewarn.Checked = FlashWarnings;

				Console.WriteLine("~ Warning flashes enabled: {0}", FlashWarnings);
			});
			togglewarn.Checked = true;

			ContextMenu.MenuItems.Add("-");
			highpriocmo = ContextMenu.MenuItems.Add("High priority", (sender, e) =>
			{
				try
				{
					Process.GetCurrentProcess().PriorityClass = Settings.Current.SelfPriority = ProcessPriorityClass.High;
					normpriocmo.Checked = false;
					highpriocmo.Checked = true;
					lowpriocmo.Checked = false;
				}
				catch { /* NOP */ }
				Console.WriteLine("~ Self-priority set to High.");
			});
			normpriocmo = ContextMenu.MenuItems.Add("Normal priority", (sender, e) =>
			{
				try
				{
					Process.GetCurrentProcess().PriorityClass = Settings.Current.SelfPriority = ProcessPriorityClass.Normal;
					normpriocmo.Checked = true;
					highpriocmo.Checked = false;
					lowpriocmo.Checked = false;
				}
				catch { /* NOP */ }
				Console.WriteLine("~ Self-priority set to Normal.");
			});
			lowpriocmo = ContextMenu.MenuItems.Add("Low priority", (sender, e) =>
			{
				try
				{
					Process.GetCurrentProcess().PriorityClass = Settings.Current.SelfPriority = ProcessPriorityClass.BelowNormal;
					normpriocmo.Checked = false;
					highpriocmo.Checked = false;
					lowpriocmo.Checked = true;
				}
				catch { /* NOP */ }
				Console.WriteLine("~ Self-priority set to Low.");
			});

			ContextMenu.MenuItems.Add("-");
			UITimer = new System.Windows.Forms.Timer { Interval = 2000 };

			updateFreq05 = ContextMenu.MenuItems.Add("Update 0.5/s", (sender, e) =>
			{
				UITimer.Interval = 500;
				updateFreq05.Checked = true;
				updateFreq15.Checked = false;
				updateFreq25.Checked = false;
				Settings.Current.UpdateFrequency = UITimer.Interval;
			});
			updateFreq15 = ContextMenu.MenuItems.Add("Update 1.5/s", (sender, e) =>
			{
				UITimer.Interval = 1500;
				updateFreq05.Checked = false;
				updateFreq15.Checked = true;
				updateFreq25.Checked = false;
				Settings.Current.UpdateFrequency = UITimer.Interval;
			});
			updateFreq25 = ContextMenu.MenuItems.Add("Update 2.5/s", (sender, e) =>
			{
				UITimer.Interval = 2500;
				updateFreq05.Checked = false;
				updateFreq15.Checked = false;
				updateFreq25.Checked = true;
				Settings.Current.UpdateFrequency = UITimer.Interval;
			});

			ContextMenu.MenuItems.Add("-");

			var alwaysOnTop = ContextMenu.MenuItems.Add("Always on top");
			alwaysOnTop.Click += (_, _ea) =>
			{
				Settings.Current.AlwaysOnTop = TopMost = alwaysOnTop.Checked = !TopMost;
				Console.WriteLine("Topmost toggled to: " + TopMost);
			};

			MenuItem runatstart = ContextMenu.MenuItems.Add("Run at Windows startup");
			runatstart.Click += (sender, e) =>
			{
				runatstart.Checked = RunAtStart(!runatstart.Checked);
			};
			runatstart.Checked = RunAtStart(true, true);

			ContextMenu.MenuItems.Add("-");
			ContextMenu.MenuItems.Add("Exit", (sender, e) => Close());

			//Console.WriteLine("Total physical memory: {0:N2} GiB", TotalMemoryMB/1000);

			UITimer.Tick += (sender, e) => UpdateSensors();

			UpdateSensors();
			UITimer.Enabled = true;
			Console.WriteLine("Timer started.");

			//CenterToScreen();

			StartPosition = FormStartPosition.CenterScreen;

			Point startLocation = Settings.Current.StartLocation;
			if (!startLocation.IsEmpty)
			{
				Location = startLocation;
				StartPosition = FormStartPosition.Manual;
			}

			alwaysOnTop.Checked = TopMost = Settings.Current.AlwaysOnTop;
			Console.WriteLine("Topmost: " + TopMost.ToString());

			FormClosing += (sender, e) =>
			{
				if (Settings.Current.StartLocation != Location)
				{
					Settings.Current.StartLocation = Location;
				}

				if (Settings.Current.AlwaysOnTop != TopMost)
				{
					Settings.Current.AlwaysOnTop = TopMost;
				}

				if (Settings.Current.Save())
				{
					Console.WriteLine("Start Location saved: " + Settings.Current.StartLocation.ToString());
					Console.WriteLine("Always on top saved:  " + Settings.Current.AlwaysOnTop.ToString());
					Console.WriteLine("Self priority saved:  " + Settings.Current.SelfPriority.ToString());
					Console.WriteLine();
					Console.WriteLine("Background color: " + Settings.Current.BackColor.ToString());
					Console.WriteLine("Warning color:    " + Settings.Current.WarnColor.ToString());
					Console.WriteLine("Text color:       " + Settings.Current.TextColor.ToString());
					Console.WriteLine("Graph color:      " + Settings.Current.GraphColor.ToString());
				}
			};

			Activated += (_, _ea) =>
			{
				// Refresh topmost as it is known to be finicky.
				if (TopMost)
				{
					TopMost = false;
					TopMost = true;
				}
			};

			Console.WriteLine("Start location: " + Location);
			Show();

			Console.WriteLine("Self-priority: " + Settings.Current.SelfPriority.ToSimpleInt() + " = " + Settings.Current.SelfPriority.ToString());
			switch (Settings.Current.SelfPriority)
			{
				case ProcessPriorityClass.High:
					highpriocmo.PerformClick();
					break;
				case ProcessPriorityClass.Normal:
					normpriocmo.PerformClick();
					break;
				default:
					lowpriocmo.PerformClick();
					break;
			}

			Console.WriteLine("Update Frequency: " + Settings.Current.UpdateFrequency);
			switch (Settings.Current.UpdateFrequency)
			{
				case 500:
					updateFreq05.PerformClick();
					break;
				case 1500:
					updateFreq15.PerformClick();
					break;
				default:
				case 2500:
					updateFreq25.PerformClick();
					break;
			}

			Console.WriteLine("Ready.");
		}

		public static string AppPath;

		static MainWindow mainwindow;

		static void Setup()
		{
			//Console.WriteLine(Application.LocalUserAppDataPath);
			AppPath = System.IO.Directory.GetParent(Application.LocalUserAppDataPath).ToString();
			Console.WriteLine(AppPath);

			bool OHWPresent = System.IO.File.Exists(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(
	System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName), "OpenHardwareMonitorLib.dll"));

			Console.WriteLine("OHW present: " + OHWPresent.ToString());

			mainwindow = new MainWindow(OHWPresent);
		}

		[STAThread]
		public static void Main()
		{
			Setup();

			try
			{
				System.Windows.Forms.Application.Run(mainwindow);
			}
			finally
			{
				var sensors = PerformanceCounterWrapper.Sensors;
				if (sensors != null && sensors.Count > 0)
				{
					Console.WriteLine("Cleaning Performance Counters...");
					foreach (var sensor in sensors)
					{
						sensor.Close();
						sensor.Dispose();
					}
					sensors.Clear();
					sensors = PerformanceCounterWrapper.Sensors = null;
				}
				Console.WriteLine("Exiting.");
			}
		}

		static MemoryStatusEx mem = new MemoryStatusEx { dwLength = (uint)Marshal.SizeOf(typeof(MemoryStatusEx)) };

		void UpdateMemory(out ulong freebytes, out ulong totalbytes)
		{
			GlobalMemoryStatusEx(ref mem);

			totalbytes = mem.ullTotalPhys;
			freebytes = mem.ullAvailPhys;
		}

		void UpdateGPU(out float load, out float mem)
		{
			HWmon.Update();

			float
				vramTotalGB = HWmon.GPUTotalMemory / 1024f,
				gpuMemLoad = HWmon.GPUMemoryLoad,
				vramUsed = vramTotalGB * (gpuMemLoad / 100f),
				vramFree = vramTotalGB - vramUsed,
				gpuMemCtrl = HWmon.GPUMemCtrlLoad,
				gpuLoad = HWmon.GPULoad,
				gpuTemp = HWmon.GPUTemperature,
				gpuFanRPM = HWmon.GPUFanSpeed,
				gpuFanLoad = HWmon.GPUFanLoad,
				gpuClock = HWmon.GPUClock;

			Sensor_GPU_MEM.Chart.Add(gpuMemLoad);
			Sensor_GPU_MEM.Value.Text = $"{vramFree:N2} GiB free\n{gpuMemLoad:N1} % usage\n{gpuMemCtrl:N1} % Ctrl";

			Sensor_GPU_Load.Chart.Add(gpuLoad);
			Sensor_GPU_Load.Value.Text = $"{gpuLoad:N1} %\n{gpuTemp:N1} C\n{gpuClock:N0} MHz\n{gpuFanRPM:N0} RPM [{gpuFanLoad:N0}%]";

			load = gpuLoad / 10f;
			mem = ((gpuMemLoad / 10f) - 7f).Min(0f) + (gpuMemCtrl / 10f);
		}

		// https://docs.microsoft.com/en-us/windows/desktop/api/sysinfoapi/ns-sysinfoapi-_memorystatusex
		[System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
		internal struct MemoryStatusEx
		{
			// size of the structure in bytes. Used by C functions
			public uint dwLength;

			/// <summary>
			/// 0 to 100, percentage of memory usage
			/// </summary>
			public uint dwMemoryLoad;

			/// <summary>
			/// Total size of physical memory, in bytes.
			/// </summary>
			public ulong ullTotalPhys;

			/// <summary>
			/// Size of physical memory available, in bytes.
			/// </summary>
			public ulong ullAvailPhys;

			/// <summary>
			/// Size of the committed memory limit, in bytes. This is physical memory plus the size of the page file, minus a small overhead.
			/// </summary>
			public ulong ullTotalPageFile;

			/// <summary>
			/// Size of available memory to commit, in bytes. The limit is ullTotalPageFile.
			/// </summary>
			public ulong ullAvailPageFile;

			/// <summary>
			/// Total size of the user mode portion of the virtual address space of the calling process, in bytes.
			/// </summary>
			public ulong ullTotalVirtual;

			/// <summary>
			/// Size of unreserved and uncommitted memory in the user mode portion of the virtual address space of the calling process, in bytes.
			/// </summary>
			public ulong ullAvailVirtual;

			/// <summary>
			/// Size of unreserved and uncommitted memory in the extended portion of the virtual address space of the calling process, in bytes.
			/// </summary>
			public ulong ullAvailExtendedVirtual;
		}

		// https://docs.microsoft.com/en-us/windows/desktop/api/sysinfoapi/nf-sysinfoapi-globalmemorystatusex
		[return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
		[System.Runtime.InteropServices.DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		static internal extern bool GlobalMemoryStatusEx(ref MemoryStatusEx lpBuffer);
	}
}
