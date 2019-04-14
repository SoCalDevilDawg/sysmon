//
// Settings.cs
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
using System.Diagnostics;
using System.Drawing;

namespace SystemMonitor
{
	public class Settings
	{
		public static SharpConfig.Configuration cfg;
		public static string datapath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
															   "MKAh", "SystemMonitor");

		public static void saveConfig(string configfile, SharpConfig.Configuration config)
		{
			//Console.WriteLine("Saving: " + configfile);
			System.IO.Directory.CreateDirectory(datapath);
			string targetfile = System.IO.Path.Combine(datapath, configfile);
			if (System.IO.File.Exists(targetfile))
				System.IO.File.Copy(targetfile, targetfile + ".bak", true); // backup
			config.SaveToFile(targetfile);
		}

		public static SharpConfig.Configuration loadConfig(string configfile)
		{
			string path = System.IO.Path.Combine(datapath, configfile);
			//Log.Trace("Opening: "+path);
			SharpConfig.Configuration retcfg;
			if (System.IO.File.Exists(path))
				retcfg = SharpConfig.Configuration.LoadFromFile(path);
			else
			{
				retcfg = new SharpConfig.Configuration();
				System.IO.Directory.CreateDirectory(datapath);
			}

			return retcfg;
		}

		public Settings()
		{
			Load();
		}

		bool _dirty = false;
		public bool Dirty
		{
			get
			{
				return _dirty;
			}
			set
			{
				_dirty = value;
			}
		}

		Point _StartLocation = Point.Empty;
		public Point StartLocation
		{
			get
			{
				if (_StartLocation.IsEmpty)
				{
					string t = cfg.TryGet("Core")?.TryGet("Start Location")?.StringValue ?? "0,0";
					string[] values = t.Split(new string[] { "," }, 2, StringSplitOptions.RemoveEmptyEntries);
					int x = Convert.ToInt32(values[0]), y = Convert.ToInt32(values[1]);
					_StartLocation = new Point(x, y);
				}
				return _StartLocation;
			}
			set
			{
				Dirty |= (_StartLocation != value);
				_StartLocation = value;
				cfg["Core"].GetSetDefault("Start Location", "0,0").StringValue = (_StartLocation.X + "," + _StartLocation.Y);
			}
		}

		ProcessPriorityClass _SelfPriority = ProcessPriorityClass.RealTime;
		public ProcessPriorityClass SelfPriority
		{
			get
			{
				if (_SelfPriority == ProcessPriorityClass.RealTime)
					_SelfPriority = (ProcessPriorityClass)(cfg.TryGet("Core")?.GetSetDefault("Self Priority", (int)ProcessPriorityClass.Idle)?.IntValue ?? 2);
				if (_SelfPriority == ProcessPriorityClass.RealTime)
					_SelfPriority = ProcessPriorityClass.Idle;

				return _SelfPriority;
			}
			set
			{
				Dirty |= (_SelfPriority != value);
				_SelfPriority = value;
				cfg["Core"].GetSetDefault("Self Priority", (int)ProcessPriorityClass.Idle).IntValue = (int)_SelfPriority;
			}
		}

		private int _UpdateFrequency = 0;
		public int UpdateFrequency
		{
			get
			{
				if (_UpdateFrequency == 0) _UpdateFrequency = cfg["Core"].GetSetDefault("Update Frequency", 2500).IntValue;
				return _UpdateFrequency;
			}
			set
			{
				Dirty |= (_UpdateFrequency != value);
				_UpdateFrequency = value;
				cfg["Core"].GetSetDefault("Update Frequency", 2500).IntValue = value;
			}
		}

		public void Load()
		{
			//MainWindow.AppPath
			cfg = loadConfig("Core.ini");
		}

		public void Save()
		{
			saveConfig("Core.ini", cfg);
		}
	}
}
