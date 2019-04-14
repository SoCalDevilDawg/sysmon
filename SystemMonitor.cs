//
// MainWindow.cs
//
// Author:
//       M.A. (https://github.com/mkahvi)
//
// Copyright (c) 2017 M.A.
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
using System.Drawing;
using System.Linq;
using System.Management;
using System.Windows.Forms;

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

		const float freememthresholddefault = 1.5f;
		const float freememthresholdmin = 0.5f;
		const float freememthresholdmax = 4f;
		float freememthershold = freememthresholddefault;
		const float memorypressuredefault = 0.9f;
		const float memorypressuremin = 0.5f;
		const float memorypressuremax = 0.99f;
		float MemoryPressureThreshold = memorypressuredefault;

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
					Processes.Add(name, (current = new ProcessPFC(name)));
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

		void UpdateSensors()
		{
			var cpuusaget = cpuusage.Value;
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

			if (float.IsNaN(cpuusaget) || float.IsNaN(cpuqueuet) || float.IsNaN(interruptt))
			{
				// TODO: Handle this
			}

			//Sensor_CPU.Value.Text = string.Format("{0:N1}%\n{1}\n{2} queued\n{3:N1}% interrupt", cpuusaget, coreusagett, cpuqueuet, interruptt);
			Sensor_CPU.Value.Text = string.Format("{0:N1}%\n{1} queued\n{2:N1}% interrupt", cpuusaget, cpuqueuet, interruptt);
			// BOTTLENECK :: CPU
			float cpucurve = (Convert.ToSingle(Math.Pow(((cpuusaget / 100f) + 0.4f), 7.1f)) - ((cpuusaget / 100f) / 10f)).LimitRange(0f, 10f);
			bottleneck_cpu = cpucurve + (cpuqueuet / 2) + (interruptt / 4);
			Sensor_CPU.Chart.Add(cpuusaget);

			// Actual as reported, and memory as determined programmatically. The difference is likely disk caching space.
			// Memory pressure going over the "true" value is likely going to result in poor performance.
			float privmem = privatememory.Value;
			float memfreet = memfree.Value / 1024; // free memory in GB
			float mempressure = (privmem / TotalMemory);
			float memfreert = (TotalMemory - privmem) / 1024000000;
			Sensor_Memory.Value.Text = string.Format("{0:N2} GiB free\n{1:N1}% commit\n{2:N1}% pressure",
													 memfreet, memcommit.Value, mempressure * 100);
			var tempmem = (TotalMemoryMB) - (memfreet * 1000);
			Sensor_Memory.Chart.Add(tempmem);

			// BOTTLENECK :: MEM
			bottleneck_mem =
				(mempressure / 10) // memory pressure default by %
				+ (Math.Max(0, LowMemThreshold - memfreet) * LowMemMultiplier);

			var splitiot = splitio.Value;
			var highavgt = Math.Max(avgnvmread.Value * 1000, avgnvmwrite.Value * 1000);
			var nvmqueuet = nvmqueue.Value;
			var nvmtimet = nvmtime.Value;
			var nvmbytesr = nvmbytes.Value;
			var nvmbytest = nvmbytesr / 1000000;
			var nvmtransferst = nvmtransfers.Value;
			Sensor_NVMIO.Value.Text = string.Format("{0:N2} MB\n{1:N1} transfers\n{2:N1}ms delay\n{3:N2} splits\n{4} queued",
													nvmbytest, nvmtransferst, highavgt, splitiot, nvmqueuet);
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

			// TODO: Make this more dynamic
			if (bottleneck_mem >= 8)
				Sensor_Bottleneck.Value.Text = "MEM";
			else if (bottleneck_nvm >= 8)
				Sensor_Bottleneck.Value.Text = "NVM";
			else if (bottleneck_cpu >= 8)
				Sensor_Bottleneck.Value.Text = "CPU";
			else
				Sensor_Bottleneck.Value.Text = "---";
			Sensor_Bottleneck.Value.Text += string.Format("\nMEM: {0:N1}\nNVM: {1:N1}\nCPU: {2:N1}", bottleneck_mem, bottleneck_nvm, bottleneck_cpu);

			if (FlashWarnings)
			{
				if (cpuusaget >= 85.0 || cpuqueuet >= 5.0) Sensor_CPU.Warn();

				if ((memfreet <= freememthershold) || (mempressure >= (MemoryPressureThreshold))) Sensor_Memory.Warn();

				if (splitiot >= 2 || highavgt >= 45 || nvmtimet >= 20) Sensor_NVMIO.Warn();

				if (Math.Max(Math.Max(bottleneck_cpu, bottleneck_nvm), bottleneck_mem) >= 6) Sensor_Bottleneck.Warn();
			}

			// CheckLoaders();
		}

		PerformanceCounterWrapper cpuusage;
		//PerformanceCounterWrapper[] coreusage;
		PerformanceCounterWrapper cpuqueue;
		PerformanceCounterWrapper interrupt;

		PerformanceCounterWrapper memfree;
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

			cpuusage = new PerformanceCounterWrapper("Processor", "% Processor Time", "_Total");
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

			memfree = new PerformanceCounterWrapper("Memory", "Available MBytes", null);
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

			Console.WriteLine("Initialization complete.");
		}

		ulong TotalMemory = 0;
		ulong TotalMemoryMB = 0;

		SensorChunk Sensor_Bottleneck;
		SensorChunk Sensor_CPU;
		SensorChunk Sensor_Memory;
		SensorChunk Sensor_PageFault;
		SensorChunk Sensor_NVMIO;
		SensorChunk Sensor_NetIO;

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

		MenuItem lowpriocmo;
		MenuItem normpriocmo;
		MenuItem highpriocmo;
		MenuItem togglewarn;

		MenuItem updateFreq05;
		MenuItem updateFreq15;
		MenuItem updateFreq25;

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

		Settings uSettings = new Settings();

		public MainWindow()
		{
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
			BackColor = Color.DarkSlateGray;

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
			var settings = System.Configuration.ConfigurationManager.AppSettings;
			string freememthresholdkey = "Free memory threshold";
			if (settings[freememthresholdkey] == null) settings.Add(freememthresholdkey, (freememthresholddefault).ToString());
			freememthershold = Convert.ToSingle(settings.Get(freememthresholdkey)).LimitRange(freememthresholdmin, freememthresholdmax);
			Console.WriteLine("+ Free mem threshold: {0}", freememthershold);
			string memorypressurekey = "Memory pressure";
			if (settings[memorypressurekey] == null) settings.Add(memorypressurekey, (memorypressuredefault).ToString());
			MemoryPressureThreshold = Convert.ToSingle(settings.Get(memorypressurekey)).LimitRange(memorypressuremin, memorypressuremax);
			Console.WriteLine("+ Memory pressure:    {0}", MemoryPressureThreshold);
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

			Sensor_CPU = new SensorChunk("CPU", true);
			Sensor_CPU.Chart.MaxValue = 100.0; // 100%
			Sensor_CPU.Chart.StaticRange = true;
			tooltip.SetToolTip(Sensor_CPU.Value, "Queued command counter is clearest indicator of underscaled CPU.\nInterrupt percentage shows load from peripherals, NICs, extension cards, and such.");

			Sensor_Memory = new SensorChunk("Memory", true, horizontal: true);
			tooltip.SetToolTip(Sensor_Memory.Value, "Physical memory usage.\nCommit is swap file usage.\nPressure is private memory load.");
			Sensor_Memory.Chart.MaxValue = TotalMemoryMB;

			Sensor_PageFault = new SensorChunk("Page Faults", true);
			tooltip.SetToolTip(Sensor_PageFault.Value, "Page file performance degradation.\nPage faults themselves are not to worry.\nHard page faults can be source of poor performance.");

			Sensor_NVMIO = new SensorChunk("NVM", true);
			tooltip.SetToolTip(Sensor_NVMIO.Header, "Non-volatile memory: HDD, SSD, etc.\nThese are not clear indicators of bottlenecks if multiple NVMs are involved.");
			tooltip.SetToolTip(Sensor_NVMIO.Value, "Split indicates fragmentation performance loss.\nQueue&delay indicates slow NVM.");

			Sensor_NetIO = new SensorChunk("Network", true);
			tooltip.SetToolTip(Sensor_NetIO.Value, "Queue length is indicator of too slow or bad outbound connection.");

			layout.Controls.Add(Sensor_Bottleneck);
			layout.Controls.Add(Sensor_CPU);
			layout.Controls.Add(Sensor_Memory);
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
					Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;
					normpriocmo.Checked = false;
					highpriocmo.Checked = true;
					lowpriocmo.Checked = false;
					uSettings.SelfPriority = ProcessPriorityClass.High;
				}
				catch { /* NOP */ }
				Console.WriteLine("~ Self-priority set to High.");
			});
			normpriocmo = ContextMenu.MenuItems.Add("Normal priority", (sender, e) =>
			{
				try
				{
					Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.Normal;
					normpriocmo.Checked = true;
					highpriocmo.Checked = false;
					lowpriocmo.Checked = false;
					uSettings.SelfPriority = ProcessPriorityClass.Normal;
				}
				catch { /* NOP */ }
				Console.WriteLine("~ Self-priority set to Normal.");
			});
			lowpriocmo = ContextMenu.MenuItems.Add("Low priority", (sender, e) =>
			{
				try
				{
					Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.Idle;
					normpriocmo.Checked = false;
					highpriocmo.Checked = false;
					lowpriocmo.Checked = true;
					uSettings.SelfPriority = ProcessPriorityClass.Idle;
				}
				catch { /* NOP */ }
				Console.WriteLine("~ Self-priority set to Low.");
			});

			lowpriocmo.Checked = true;

			ContextMenu.MenuItems.Add("-");
			var n = new System.Windows.Forms.Timer { Interval = 2000 };

			updateFreq05 = ContextMenu.MenuItems.Add("Update 0.5/s", (sender, e) =>
			{
				n.Interval = 500;
				updateFreq05.Checked = true;
				updateFreq15.Checked = false;
				updateFreq25.Checked = false;
				uSettings.UpdateFrequency = n.Interval;
			});
			updateFreq15 = ContextMenu.MenuItems.Add("Update 1.5/s", (sender, e) =>
			{
				n.Interval = 1500;
				updateFreq05.Checked = false;
				updateFreq15.Checked = true;
				updateFreq25.Checked = false;
				uSettings.UpdateFrequency = n.Interval;
			});
			updateFreq25 = ContextMenu.MenuItems.Add("Update 2.5/s", (sender, e) =>
			{
				n.Interval = 2500;
				updateFreq05.Checked = false;
				updateFreq15.Checked = false;
				updateFreq25.Checked = true;
				uSettings.UpdateFrequency = n.Interval;
			});

			ContextMenu.MenuItems.Add("-");
			MenuItem runatstart = ContextMenu.MenuItems.Add("Run at Windows startup");
			runatstart.Click += (sender, e) =>
			{
				runatstart.Checked = RunAtStart(!runatstart.Checked);
			};
			runatstart.Checked = RunAtStart(true, true);
			ContextMenu.MenuItems.Add("-");
			ContextMenu.MenuItems.Add("Exit", (sender, e) => Close());

			//Console.WriteLine("Total physical memory: {0:N2} GiB", TotalMemoryMB/1000);

			n.Tick += (sender, e) => UpdateSensors();

			UpdateSensors();
			n.Enabled = true;
			Console.WriteLine("Timer started.");

			//CenterToScreen();

			StartPosition = FormStartPosition.CenterScreen;

			Point startLocation = uSettings.StartLocation;
			if (!startLocation.IsEmpty)
			{
				Location = startLocation;
				StartPosition = FormStartPosition.Manual;
			}

			FormClosing += (sender, e) =>
			{
				if (uSettings.StartLocation != Location)
				{
					uSettings.StartLocation = Location;
				}

				if (uSettings.Dirty)
				{
					uSettings.Save();
					Console.WriteLine("Start Location saved: " + uSettings.StartLocation);
					Console.WriteLine("Self priority saved:  " + uSettings.SelfPriority);
				}
			};

			Console.WriteLine("Start location: " + Location);
			Show();

			Console.WriteLine("Self-priority: " + uSettings.SelfPriority);
			switch (uSettings.SelfPriority)
			{
				case ProcessPriorityClass.High:
					highpriocmo.PerformClick();
					break;
				case ProcessPriorityClass.Normal:
					normpriocmo.PerformClick();
					break;
				case ProcessPriorityClass.Idle:
				default:
					lowpriocmo.PerformClick();
					break;
			}

			Console.WriteLine("Update Frequency: " + uSettings.UpdateFrequency);
			switch (uSettings.UpdateFrequency)
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

		[STAThread]
		public static void Main()
		{
			Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.Idle; // set self to low priority

			//Console.WriteLine(Application.LocalUserAppDataPath);
			AppPath = System.IO.Directory.GetParent(Application.LocalUserAppDataPath).ToString();
			Console.WriteLine(AppPath);

			try
			{
				MainWindow win = null;
				win = new MainWindow();
				if (win != null) System.Windows.Forms.Application.Run(win);
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
	}
}
