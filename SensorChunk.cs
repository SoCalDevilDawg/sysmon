//
// SensorChunk.cs
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
using System.Windows.Forms;

namespace SystemMonitor
{
	public class SensorChunk
	{
		public TableLayoutPanel Layout { get; private set; }
		public SensorHeader Header { get; set; }
		public SensorValue Value { get; set; }
		public SensorChart Chart { get; set; }

		public int Height { get; set; }
		public int Width { get; set; }

		bool Horizontal = false;

		public static implicit operator Control(SensorChunk chart)
		{
			return chart.Layout;
		}

		public SensorChunk(string title, bool chart = false, int width = 160, int height = 80, bool horizontal = false)
		{
			Width = width;
			Height = height;

			Horizontal = horizontal;

			Layout = new TableLayoutPanel
			{
				ColumnCount = 1,
				RowCount = 2,
				BackColor = Color.Transparent,
				Width = Width,
				AutoSize = true,
				Margin = new Padding(0),
				Padding = new Padding(0),
			};

			Header = new SensorHeader { Text = title, Height = 26, Width = Width };
			Value = new SensorValue { Width = Header.Width, Height = Height };

			Layout.Controls.Add(Header, 0, 0);
			if (chart)
			{
				Chart = new SensorChart(title, Height, Width, Horizontal);
				Chart.MinValue = 0.0d;

				Value.Parent = Chart;
				Layout.Controls.Add(Chart, 0, 1);
			}
			else
				Layout.Controls.Add(Value, 0, 1);

			wth = new Timer
			{
				Interval = 2100
			};
			wth.Tick += (_, _ea) => Normal();
			wth.Enabled = true;
			wth.Stop();
		}

		readonly Timer wth;

		public void Warn()
		{
			wth.Stop();
			wth.Start();
			Layout.BackColor = Settings.Current.WarnColor;
		}

		public void Normal()
		{
			wth.Stop();
			Layout.BackColor = Color.Transparent;
		}
	}

	public class SensorHeader : Label
	{
		/*
		protected Color OutlineColor = Color.Black;
		protected float OutlineWidth = 2;

		protected override void OnPaint(PaintEventArgs e)
		{
			e.Graphics.FillRectangle(new SolidBrush(BackColor), ClientRectangle);
			using (System.Drawing.Drawing2D.GraphicsPath gp = new System.Drawing.Drawing2D.GraphicsPath())
			using (Pen outline = new Pen(OutlineColor, OutlineWidth)
			{ LineJoin = System.Drawing.Drawing2D.LineJoin.Round })
			using (StringFormat sf = new StringFormat() )
			using (Brush foreBrush = new SolidBrush(ForeColor))
			{
				gp.AddString(Text, Font.FontFamily, (int)Font.Style,
					Font.Size, ClientRectangle, sf);
				e.Graphics.ScaleTransform(1.3f, 1.35f);
				e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
				e.Graphics.DrawPath(outline, gp);
				e.Graphics.FillPath(foreBrush, gp);
			}
		}
		*/

		public SensorHeader()
		{
			BackColor = Color.Transparent;
			ForeColor = Settings.Current.TextColor;

			Text = "N/A";

			Dock = DockStyle.Left;
			TextAlign = System.Drawing.ContentAlignment.TopCenter;
			//BackColor = NormalColor;
			Height = 20;

			Font = new Font("Arial", 12f, FontStyle.Bold);
		}
	}

	public class SensorValue : SensorHeader
	{
		public SensorValue()
		{
			Font = new Font("Verdana", 8f, FontStyle.Regular);
		}
	}

	public class SensorChart : IDisposable
	{
		public static implicit operator Control(SensorChart chart)
		{
			return chart.Control;
		}

		public System.Windows.Forms.DataVisualization.Charting.Chart Chart { get; private set; }
		public System.Windows.Forms.DataVisualization.Charting.Series Series { get; private set; }
		public System.Windows.Forms.DataVisualization.Charting.ChartArea ChartArea { get; private set; }

		bool Horizontal = false;

		int MaxPoints = 25;

		public Control Control
		{
			get
			{
				return Chart;
			}
		}

		int ReductionCounter = 0;
		public void Add(double newvalue)
		{
			try
			{
				Chart.SuspendLayout();

				Series.Points.AddY(newvalue);

				if ((Horizontal && Series.Points.Count > 1) || Series.Points.Count > MaxPoints) Series.Points.RemoveAt(0);
				if (!StaticRange)
				{
					if (MaxValue < newvalue)
						MaxValue = newvalue + double.Epsilon;
					else if (newvalue < (MaxValue * .8))
					{
						ReductionCounter++;
						if (ReductionCounter > (MaxPoints / 2))
						{
							MaxValue *= .8;
							ReductionCounter = 0;
						}
					}
					else
						ReductionCounter = 0;
				}

				Chart.ResumeLayout();
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex.Message);
				Debug.WriteLine(ex.StackTrace);
			}
		}

		public double MaxValue
		{
			get => ChartArea.AxisY.Maximum;
			set => ChartArea.AxisY.Maximum = value + double.Epsilon;
		}

		public double MinValue
		{
			get => ChartArea.AxisY.Minimum;
			set => ChartArea.AxisY.Minimum = value;
		}

		public int Maximum
		{
			get => MaxPoints;
			set
			{
				MaxPoints = value;
				ChartArea.AxisX.Maximum = (Horizontal ? MaxPoints : MaxPoints + 1);
			}
		}

		public bool StaticRange = false;

		public SensorChart(string name, int height, int width, bool horizontal = false)
		{
			Horizontal = horizontal;

			Chart = new System.Windows.Forms.DataVisualization.Charting.Chart
			{
				BackColor = Color.Transparent,
				Dock = DockStyle.Fill,
				//ForeColor = Color.Blue,
				BackHatchStyle = System.Windows.Forms.DataVisualization.Charting.ChartHatchStyle.None,
				Height = height,
				Width = width,
				Padding = Padding.Empty,
			};
			var skey = name;
			Series = new System.Windows.Forms.DataVisualization.Charting.Series
			{
				Name = skey,
				Color = Settings.Current.GraphColor,
				BorderColor = Color.Transparent,
				IsVisibleInLegend = false,
				IsXValueIndexed = true,
				//AxisLabel = "",
				MarkerStyle = System.Windows.Forms.DataVisualization.Charting.MarkerStyle.None,
				MarkerColor = Color.Transparent,
				//MarkerStep = MaxValues,
				IsValueShownAsLabel = false,
				//BackGradientStyle = GradientStyle.TopBottom,
				//BackSecondaryColor = Color.Transparent,
				XValueType = System.Windows.Forms.DataVisualization.Charting.ChartValueType.Int32,
				BackHatchStyle = System.Windows.Forms.DataVisualization.Charting.ChartHatchStyle.None,
				YValuesPerPoint = 1,
				ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Column,
			};

			if (horizontal)
			{
				Series.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Bar;
				MaxPoints = 9;
			}

			//var cpulegend = new Legend(skey);
			Chart.Series.Add(Series);
			//cpuchart.Legends.Add(cpulegend);
			ChartArea = new System.Windows.Forms.DataVisualization.Charting.ChartArea
			{
				BackColor = Color.Transparent,
				AxisX = new System.Windows.Forms.DataVisualization.Charting.Axis
				{
					LabelStyle = new System.Windows.Forms.DataVisualization.Charting.LabelStyle { Enabled = false },
					Maximum = (horizontal ? MaxPoints : MaxPoints + 1),
					Minimum = 0,
				},
				AxisY = new System.Windows.Forms.DataVisualization.Charting.Axis
				{
					LabelStyle = new System.Windows.Forms.DataVisualization.Charting.LabelStyle { Enabled = false },
				},
				BorderWidth = 0,
			};
			ChartArea.AxisY.IsMarginVisible = false;
			ChartArea.AxisY.MajorGrid.Enabled = false;
			ChartArea.AxisY.MinorGrid.Enabled = false;
			ChartArea.AxisY.MinorTickMark.Enabled = false;
			ChartArea.AxisY.MajorTickMark.Enabled = false;
			ChartArea.AxisY.LineColor = Color.Transparent;
			ChartArea.AxisX.IsMarginVisible = false;
			ChartArea.AxisX.MajorGrid.Enabled = false;
			ChartArea.AxisX.MinorGrid.Enabled = false;
			ChartArea.AxisX.MinorTickMark.Enabled = false;
			ChartArea.AxisX.MajorTickMark.Enabled = false;
			ChartArea.AxisX.LineColor = Color.Transparent;
			//cpuchartarea.AxisX.ScaleBreakStyle = new AxisScaleBreakStyle { BreakLineStyle = BreakLineStyle.None },

			Chart.ChartAreas.Add(ChartArea);

			for (var i = 0; i < MaxPoints; i++)
				Add(0f);
		}

		// IDISPOSE
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
				Chart.Dispose();
				ChartArea.Dispose();
				Series.Dispose();

				disposed = true;
			}
		}
	}

	/*
	public class SensorChunk : IDisposable
	{
		List<PerformanceCounterWrapper> PFCs = new List<PerformanceCounterWrapper>(1);

		Label _header;
		TableLayoutPanel _layout;

		Color NormalColor = Color.LightYellow;
		Color WarnColor = Color.Orange;

		public SensorChunk(string name, Control parent, int frequency=2000)
		{
			_layout = new TableLayoutPanel
			{
				Parent=parent,
				ColumnCount = 1,
				RowCount = 2,
			};

			_header = new Label { Text = name, TextAlign = ContentAlignment.MiddleCenter, BackColor=NormalColor };
			_layout.Controls.Add(_header);
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
				foreach (var pfc in PFCs) pfc.Dispose();
				PFCs.Clear();

				_layout.Dispose();
				_layout = null;
				_header.Dispose();
				_header = null;

				disposed = true;
			}
		}
	}
	*/
}
