#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

//This namespace holds Indicators in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Indicators
{
	public class TestSupplyZone : Indicator
	{
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Enter the description for your new custom Indicator here.";
				Name										= "TestSupplyZone";
				Calculate									= Calculate.OnEachTick;
				IsOverlay									= true;
				DisplayInDataBox							= true;
				DrawOnPricePanel							= true;
				DrawHorizontalGridLines						= true;
				DrawVerticalGridLines						= true;
				PaintPriceMarkers							= true;
				ScaleJustification							= NinjaTrader.Gui.Chart.ScaleJustification.Right;
				IsSuspendedWhileInactive					= true;
			}
			else if (State == State.Configure)
			{
			}
		}

		protected override void OnBarUpdate()
		{
			if(State == State.Realtime)
			{
				try
				{
					int    b = Swing(10).SwingHighBar(1, 1, 100);
					double h = High[b];
					double l = Low[b];
					
					SupplyZone sz = Draw.SupplyZone(this, "sz_" + (CurrentBar -b), false, b, h, b, l, Brushes.Coral, 15, Brushes.Coral, 2, 60, DashStyleHelper.Solid, "M5", 0, Brushes.Coral, 30, 0, true, true, "");
				}
				catch(Exception e)
				{
					Print("TestSupplyZone: " + e.ToString());
				}
			}
		}
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private TestSupplyZone[] cacheTestSupplyZone;
		public TestSupplyZone TestSupplyZone()
		{
			return TestSupplyZone(Input);
		}

		public TestSupplyZone TestSupplyZone(ISeries<double> input)
		{
			if (cacheTestSupplyZone != null)
				for (int idx = 0; idx < cacheTestSupplyZone.Length; idx++)
					if (cacheTestSupplyZone[idx] != null &&  cacheTestSupplyZone[idx].EqualsInput(input))
						return cacheTestSupplyZone[idx];
			return CacheIndicator<TestSupplyZone>(new TestSupplyZone(), input, ref cacheTestSupplyZone);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.TestSupplyZone TestSupplyZone()
		{
			return indicator.TestSupplyZone(Input);
		}

		public Indicators.TestSupplyZone TestSupplyZone(ISeries<double> input )
		{
			return indicator.TestSupplyZone(input);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.TestSupplyZone TestSupplyZone()
		{
			return indicator.TestSupplyZone(Input);
		}

		public Indicators.TestSupplyZone TestSupplyZone(ISeries<double> input )
		{
			return indicator.TestSupplyZone(input);
		}
	}
}

#endregion
