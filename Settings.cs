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
		public static readonly string datapath = System.IO.Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MKAh", "SystemMonitor");

		public static Settings Current = new Settings();

		public static Ini.Config config;

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
			Debug.WriteLine("Data path: " + datapath);
			Debug.WriteLine("Data file: " + filename);
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

		Point? _StartLocation = null;
		public Point StartLocation
		{
			get
			{
				if (!_StartLocation.HasValue)
				{
					string t = config.Get("Core")?.Get("Start Location")?.String ?? "0,0";
					string[] values = t.Split(new string[] { "," }, 2, StringSplitOptions.RemoveEmptyEntries);
					int x = Convert.ToInt32(values[0]), y = Convert.ToInt32(values[1]);
					_StartLocation = new Point(x, y);
				}

				return _StartLocation.Value;
			}
			set
			{
				StartLocation = value;
				config["Core"]["Start Location"].String = $"{value.X},{value.Y}";
			}
		}

		ProcessPriorityClass? _processpriority = null;

		public ProcessPriorityClass SelfPriority
		{
			get
			{
				if (!_processpriority.HasValue)
					_processpriority = ProcessPriority.FromInt(config["Core"].GetOrSet("Self priority", ProcessPriorityClass.BelowNormal.ToSimpleInt()).Int.Constrain(0, 4));

				return _processpriority.Value;
			}
			set
			{
				_processpriority = value;
				config["Core"]["Self Priority"].Int = value.ToSimpleInt();
			}
		}

		int? _updatefrequency = null;

		public int UpdateFrequency
		{
			get
			{
				if (!_updatefrequency.HasValue)
					_updatefrequency = config["Core"].GetOrSet("Update frequency", 2500).Int.Constrain(100, 5000);

				return _updatefrequency.Value;
			}
			set
			{
				_updatefrequency = value;
				config["Core"]["Update frequency"].Int = value;
			}
		}

		double? _freememorythreshold = null;

		public double FreeMemoryThreshold
		{
			get
			{
				if (!_freememorythreshold.HasValue)
					_freememorythreshold = config["Core"].GetOrSet("Free memory threshold", 1.5d).Double.Constrain(0.5d, 4d);

				return _freememorythreshold.Value;
			}
			set
			{
				_freememorythreshold = value;
				config["Core"]["Free memory threshold"].Double = value;
			}
		}

		double? _memorypressurethreshold = null;

		public double MemoryPressureThreshold
		{
			get
			{
				if (!_memorypressurethreshold.HasValue)
					_memorypressurethreshold = config["Core"].GetOrSet("Memory pressure threshold", 0.9d).Double.Constrain(0.5d, 0.99d);

				return _memorypressurethreshold.Value;
			}
			set
			{
				_memorypressurethreshold = value;
				config["Core"]["Memory pressure threshold"].Double = value;
			}
		}

		const string CoreConfigFile = "Core.ini";

		public void Load() => config = LoadConfig(CoreConfigFile);

		public bool Save()
		{
			if (config.Changes > 0)
			{
				SaveConfig(CoreConfigFile, config);
				return true;
			}

			return false;
		}
	}
}
