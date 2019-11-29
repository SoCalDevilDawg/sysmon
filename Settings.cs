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

using MKAh;
using System;
using System.Diagnostics;
using System.Drawing;
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

		bool? _alwaysontop = null;
		public bool AlwaysOnTop
		{
			get
			{
				if (!_alwaysontop.HasValue)
				{
					_alwaysontop = config.Get("Core")?.Get("Always on top")?.Bool ?? false;
				}

				return _alwaysontop.Value;
			}
			set
			{
				config["Core"]["Always on top"].Bool = value;

				_alwaysontop = value;
			}
		}

		string ColorToString(Color value) => $"{value.R.ToString()},{value.G.ToString()},{value.B.ToString()}";

		Color StringToColor(string value)
		{
			string[] values = value.Trim().Split(new string[] { "," }, 3, StringSplitOptions.RemoveEmptyEntries);
			int r = Convert.ToInt32(values[0].Trim()), g = Convert.ToInt32(values[1].Trim()), b = Convert.ToInt32(values[2].Trim());
			return Color.FromArgb(r, g, b);
		}

		void WriteStack(Exception ex)
		{
			Debug.WriteLine("- - - - -");
			Debug.WriteLine("EXCEPTION");
			Debug.WriteLine(ex.Message);
			Debug.WriteLine("- - - - -");
			Debug.WriteLine(ex.StackTrace);
			Debug.WriteLine("- - - - -");
		}

		Color? _BackColor = null;
		public Color BackColor
		{
			get
			{
				if (!_BackColor.HasValue)
				{
					string? color = config.Get("Color")?.Get("Background").String;
					try
					{
						if (!string.IsNullOrEmpty(color)) _BackColor = StringToColor(color);
						else Console.WriteLine("Backcolor color configuration invalid");
					}
					catch (Exception ex)
					{
						WriteStack(ex);
					}

					if (!_BackColor.HasValue) _BackColor = Color.DarkSlateGray;
					else Console.WriteLine("Custom back color: " + _BackColor.Value.ToString());
				}

				return _BackColor.Value;
			}
			set
			{
				config["Color"]["Background"].String = ColorToString(value);
				_BackColor = value;
			}
		}

		Color? _WarnColor = null;
		public Color WarnColor
		{
			get
			{
				if (!_WarnColor.HasValue)
				{
					string? color = config.Get("Color")?.Get("Warning").String;
					try
					{
						if (!string.IsNullOrEmpty(color)) _WarnColor = StringToColor(color);
						else Console.WriteLine("Warning color configuration invalid");
					}
					catch (Exception ex)
					{
						WriteStack(ex);
					}

					if (!_WarnColor.HasValue) _WarnColor = Color.IndianRed;
					else Console.WriteLine("Custom warning color: " + _WarnColor.Value.ToString());
				}

				return _WarnColor.Value;
			}
			set
			{
				config["Color"]["Warning"].String = ColorToString(value);
				_WarnColor = value;
			}
		}

		Color? _TextColor = null;
		public Color TextColor
		{
			get
			{
				if (!_TextColor.HasValue)
				{
					string? color = config.Get("Color")?.Get("Text").String;
					try
					{
						if (!string.IsNullOrEmpty(color)) _TextColor = StringToColor(color);
						else Console.WriteLine("Text color configuration invalid");
					}
					catch (Exception ex)
					{
						WriteStack(ex);
					}

					if (!_TextColor.HasValue) _TextColor = Color.White;
					else Console.WriteLine("Custom text color: " + _BackColor.Value.ToString());
				}

				return _TextColor.Value;
			}
			set
			{
				config["Color"]["Text"].String = ColorToString(value);
				_TextColor = value;
			}
		}
		Color? _GraphColor = null;
		public Color GraphColor
		{
			get
			{
				if (!_GraphColor.HasValue)
				{
					string? color = config.Get("Color")?.Get("Graphs").String;
					try
					{
						if (!string.IsNullOrEmpty(color)) _GraphColor = StringToColor(color);
						else Console.WriteLine("Graph color configuration invalid");
					}
					catch (Exception ex)
					{
						WriteStack(ex);
					}

					if (!_GraphColor.HasValue) _GraphColor = Color.RosyBrown;
					else Console.WriteLine("Custom graph color: " + _BackColor.Value.ToString());
				}

				return _GraphColor.Value;
			}
			set
			{
				config["Color"]["Graphs"].String = ColorToString(value);
				_GraphColor = value;
			}
		}

		Point? _StartLocation = null;
		public Point StartLocation
		{
			get
			{
				if (!_StartLocation.HasValue)
				{
					string t = config.Get("Core")?.Get("Start Location")?.String ?? "0,0";
					string[] values = t.Trim().Split(new string[] { "," }, 2, StringSplitOptions.RemoveEmptyEntries);
					int x = Convert.ToInt32(values[0].Trim()), y = Convert.ToInt32(values[1].Trim());
					_StartLocation = new Point(x, y);
				}

				return _StartLocation.Value;
			}
			set
			{
				_StartLocation = value;
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
