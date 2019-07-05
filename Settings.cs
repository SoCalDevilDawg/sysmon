//
// Settings.cs
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
using System.Drawing;
using MKAh;
using Ini = MKAh.Ini;

namespace SystemMonitor
{
	public class Settings
	{
		public static Ini.Config config;

		public static string datapath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
															   "MKAh", "SystemMonitor");

		public static void SaveConfig(string configfile, Ini.Config config)
		{
			//Console.WriteLine("Saving: " + configfile);
			System.IO.Directory.CreateDirectory(datapath);
			string targetfile = System.IO.Path.Combine(datapath, configfile);
			if (System.IO.File.Exists(targetfile))
				System.IO.File.Copy(targetfile, targetfile + ".bak", true); // backup
			config.SaveToFile(targetfile);
		}

		public static Ini.Config LoadConfig(string filename)
		{
			string path = System.IO.Path.Combine(datapath, filename);
			//Log.Trace("Opening: "+path);
			Ini.Config retcfg;
			if (System.IO.File.Exists(path))
				retcfg = Ini.Config.FromFile(path);
			else
			{
				retcfg = new Ini.Config();
				System.IO.Directory.CreateDirectory(datapath);
			}

			return retcfg;
		}

		public Settings() => Load();

		public bool Dirty { get; set; } = false;

		Point _StartLocation = Point.Empty;
		public Point StartLocation
		{
			get
			{
				if (_StartLocation.IsEmpty)
				{
					string t = config.Get("Core")?.Get("Start Location")?.Value ?? "0,0";
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
				config["Core"].GetOrSet("Start Location", "0,0", out _).Value = (_StartLocation.X + "," + _StartLocation.Y);
			}
		}

		ProcessPriorityClass _SelfPriority = ProcessPriorityClass.RealTime;
		public ProcessPriorityClass SelfPriority
		{
			get
			{
				if (_SelfPriority == ProcessPriorityClass.RealTime)
					_SelfPriority = (ProcessPriorityClass)(config.Get("Core")?.GetOrSet("Self Priority", (int)ProcessPriorityClass.Idle, out _)?.IntValue ?? 2);
				if (_SelfPriority == ProcessPriorityClass.RealTime)
					_SelfPriority = ProcessPriorityClass.Idle;

				return _SelfPriority;
			}
			set
			{
				Dirty |= (_SelfPriority != value);
				_SelfPriority = value;
				config["Core"].GetOrSet("Self Priority", (int)ProcessPriorityClass.Idle, out _).IntValue = (int)_SelfPriority;
			}
		}

		private int _UpdateFrequency = 0;
		public int UpdateFrequency
		{
			get
			{
				if (_UpdateFrequency == 0) _UpdateFrequency = config["Core"].GetOrSet("Update Frequency", 2500, out _).IntValue;
				return _UpdateFrequency;
			}
			set
			{
				Dirty |= (_UpdateFrequency != value);
				_UpdateFrequency = value;
				config["Core"].GetOrSet("Update Frequency", 2500, out _).IntValue = value;
			}
		}

		bool _smallbars = false;
		public bool SmallBars
		{
			get
			{
				return _smallbars = config["Core"].GetOrSet("Small bars", false, out _).BoolValue;
			}
			set
			{
				Dirty |= (_smallbars != value);
				_smallbars = value;
				config["Core"].GetOrSet("Small bars", 2500, out _).BoolValue = value;
			}
		}

		const string CoreConfigFile = "Core.ini";

		public void Load() => config = LoadConfig(CoreConfigFile);

		public void Save() => SaveConfig(CoreConfigFile, config);
	}
}
