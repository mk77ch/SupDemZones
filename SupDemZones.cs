#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
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

namespace NinjaTrader.NinjaScript.Indicators.Infinity
{
	public class SupDemZones : Indicator
	{
		#region Zone
		
		public class Zone
		{
			public double h = 0.0;   // high
			public double l = 0.0;   // low
			public int    b = 0;     // bar
			public int    e = 0;     // end
			public string n = "";	 // name
			public string t = "";    // type
			public string c = "";    // context
			public bool   f = false; // flipped
			public bool   a = true;  // active
			
			public Zone(double l, double h, int b, string t, string c, bool a)
			{
				this.l = l;
				this.h = h;
				this.b = b;
				this.t = t;
				this.c = c;
				this.a = a;
				
				if(t == "s")
				{
					this.n = b + "_" + h;
				}
				
				if(t == "d")
				{
					this.n = b + "_" + l;
				}
			}
		}
		
		#endregion
		
		#region Variables
		
		private int minTicks = 1;
		
		private int  atrPeriod = 100;
		
		// ---
		
		private int    currHiBar,currLoBar,prevHiBar,prevLoBar = 0;
		private double currHiVal,currLoVal,prevHiVal,prevLoVal = 0;
		
		private int    con;
		private double currLoCon,currHiCon;
		
		private double zr,zl,zh,br;
		private int    zb;
		private string zt,zc;
		private bool   za;
		
		private double atr;
		
		// --- //
		
		private List<Zone> Zones = new List<Zone>();
		
		#endregion
		
		#region OnStateChange
		
		protected override void OnStateChange()
		{
			if(State == State.SetDefaults)
			{
				Description					= @"";
				Name						= "SupDemZones";
				Calculate					= Calculate.OnBarClose;
				IsOverlay					= true;
				IsAutoScale 				= false;
				DrawOnPricePanel			= true;
				PaintPriceMarkers			= false;
				IsSuspendedWhileInactive	= false;
				ScaleJustification			= NinjaTrader.Gui.Chart.ScaleJustification.Right;
				IsSuspendedWhileInactive	= true;
				
				demandZoneTemplate	= "d";
				supplyZoneTemplate	= "s";
				drawGlobal			= true;
			}
			else if(State == State.Configure)
			{
				SetZOrder(-7);
			}
		}
		
		#endregion
		
		#region OnBarUpdate
		
		// OnBarUpdate
		//
		protected override void OnBarUpdate()
		{
			if(CurrentBar < 20) { return; }
			
			if(isDnSwing(3))
			{
				currHiBar = 3;
				currHiVal = High[3];
			}
			
			if(isUpSwing(3))
			{
				currLoBar = 3;
				currLoVal = Low[3];
			}
			
			atr = Instrument.MasterInstrument.RoundToTickSize(ATR(atrPeriod)[0] * 1.25);
			
			checkSupply();
			checkDemand();
			updateZones();
			
			prevHiBar = currHiBar;
			prevHiVal = currHiVal;
			
			prevLoBar = currLoBar;
			prevLoVal = currLoVal;
			
			if(CurrentBar >= Count - 2)
			{
				for(int i=0;i<Zones.Count;i++)
				{
					string templateName = (Zones[i].t == "s") ? supplyZoneTemplate : demandZoneTemplate;
					Draw.Rectangle(this, Zones[i].n, CurrentBar - Zones[i].b, Zones[i].h, -100, Zones[i].l, drawGlobal, templateName);
				}
			}
		}
		
		#endregion
		
		#region SwingMethods
		
		// isUpSwing
		//
		private bool isUpSwing(int index)
		{
			if(
			Low[index] <= Low[index-1] &&
			Low[index] <= Low[index-2] &&
			Low[index] <= Low[index-3] &&
			Low[index] <= Low[index+1] &&
			Low[index] <= Low[index+2] &&
			Low[index] <= Low[index+3] &&
			(Low[index] < Low[index-1] || Low[index] < Low[index-2] || Low[index] < Low[index-3]) &&
			(Low[index] < Low[index+1] || Low[index] < Low[index+2] || Low[index] < Low[index+3])
			) {
				return true;
			}
			
			return false;
		}
		
		// isDnSwing
		//
		private bool isDnSwing(int index)
		{
			if(
			High[index] >= High[index-1] &&
			High[index] >= High[index-2] &&
			High[index] >= High[index-3] &&
			High[index] >= High[index+1] &&
			High[index] >= High[index+2] &&
			High[index] >= High[index+3] &&
			(High[index] > High[index-1] || High[index] > High[index-2] || High[index] > High[index-3]) &&
			(High[index] > High[index+1] || High[index] > High[index+2] || High[index] > High[index+3])
			) {
				return true;
			}
			
			return false;
		}
		
		#endregion
		
		#region Supply
		
		// checkSupply
		//
		private void checkSupply()
		{
			// Regular
			
			if(currHiVal != prevHiVal)
			{
				if(MAX(High, currHiBar)[0] <= currHiVal)
				{
					if(!activeSupplyZoneExists(currHiVal) && isValidSupplyZone(currHiVal, currLoVal))
					{
						br = High[currHiBar] - Low[currHiBar];
						zr = High[currHiBar] - Math.Min(Open[currHiBar], Close[currHiBar]);
						zh = currHiVal;
					 	zl = (zr > atr) ? Math.Max(Open[currHiBar], Close[currHiBar]): Math.Min(Open[currHiBar], Close[currHiBar]);
						zl = (zh - zl > atr) ? (zh - atr) : zl;
						zl = (br < atr * 0.75) ? Low[currHiBar] : zl;
						zl = (Math.Abs(zh - zl) < minTicks * TickSize) ? zh - minTicks * TickSize : zl;
						zb = CurrentBar - currHiBar;
						zt = "s";
						zc = "r";
						za = true;
						
						Zones.Add(new Zone(zl, zh, zb, zt, zc, za));
					}
				}
			}
			
			// Continuation
			
			con = isDnContinuation();
			
			if(con != -1)
			{
				currHiCon = MAX(High, con)[0];
				currLoCon = MIN(Low, con)[1];
				
				if(currHiCon - currLoCon <= atr)
				{
					if(!activeSupplyZoneExists(currHiCon) && isValidSupplyZone(currHiCon, currLoCon))
					{
						zl = currLoCon;
						zh = currHiCon;
						zb = CurrentBar - (con);
						zt = "s";
						zc = "c";
						za = true;
						
						zl = (zh - zl < TickSize) ? (zh - TickSize) : zl;
						
						Zones.Add(new Zone(zl, zh, zb, zt, zc, za));
					}
				}
			}
		}
		
		// getNextSupplyZone
		//
		private int getNextSupplyZone(double price)
		{
			double min = double.MaxValue;
			int    ind = -1;
			
			for(int i=0;i<Zones.Count;i++)
			{
				if(Zones[i].a == true && Zones[i].t == "s")
				{
					if(Zones[i].l > price && Zones[i].l < min)
					{
						ind = i;
					}
				}
			}
			
			return ind;
		}
		
		// activeSupplyZoneExists
		//
		private bool activeSupplyZoneExists(double hi)
		{
			bool exists = false;
			
			for(int i=0;i<Zones.Count;i++)
			{
				if(Zones[i].a == true && Zones[i].t == "s")
				{
					if(Zones[i].h == hi)
					{
						exists = true;
						break;
					}
				}
			}
			
			return exists;
		}
		
		// isValidSupplyZone
		//
		private bool isValidSupplyZone(double hi, double lo)
		{
			bool valid = true;
			
			for(int i=0;i<Zones.Count;i++)
			{
				if(Zones[i].a == true && Zones[i].t == "s")
				{
					if(
					(hi <= Zones[i].h && hi >= Zones[i].l) ||
					(lo <= Zones[i].h && lo >= Zones[i].l)
					) {
						valid = false;
						break;
					}
				}
			}
			
			return valid;
		}
		
		// isDnContinuation
		//
		private int isDnContinuation()
		{
			bool val = true;
			int  bar = -1;
			
			for(int i=10;i>=2;i--)
			{
				if(isDnMove(i))
				{
					val = true;
					
					for(int j=i;j>=1;j--)
					{
						if(!isInsideDnBar(j, i))
						{
							val = false;
							break;
						}
					}
					
					if(val)
					{
						val = false;
						
						for(int j=i;j>=1;j--)
						{
							if(Close[j] >= Open[j])
							{
								val = true;
								break;
							}
						}
					}
					
					if(val)
					{
						if(isInsideDnBreakoutBar(0, i))
						{
							bar = i;
							break;
						}
					}
				}
			}
			
			return bar;
		}
		
		// isDnMove
		//
		private bool isDnMove(int index)
		{
			if(
			Close[index]   < KeltnerChannel(1.0, 10).Lower[index]   ||
			Close[index+1] < KeltnerChannel(1.0, 10).Lower[index+1] ||
			Close[index+2] < KeltnerChannel(1.0, 10).Lower[index+2]
			) {
				if(
				isDnBar(index) &&
				isDnBar(index+1) &&
				isDnBar(index+2)
				) {
					return true;
				}
				
				if(
				isStrongDnBar(index)
				) {
					return true;
				}
			}
			
			return false;
		}
		
		// isDnBar
		//
		private bool isDnBar(int index)
		{
			if(
			Close[index] < Open[index] &&
			Close[index] < Close[index+1] &&
			High[index]  < High[index+1] &&
			Low[index]   < Low[index+1]
			) {
				return true;
			}
			
			return false;
		}
		
		// isStrongDnBar
		//
		private bool isStrongDnBar(int index)
		{
			if(
			Close[index] < Open[index] &&
			Close[index] < Close[index+1] &&
			High[index]  < High[index+1] &&
			Low[index]   < Low[index+1] &&
			Low[index]   < MIN(Low, 3)[index+1] &&
			High[index]  - Low[index] > ATR(atrPeriod)[1]
			) {
				return true;
			}
			
			if(
			Close[index] < Open[index] &&
			Close[index] < Close[index+1] &&
			Close[index] < MIN(Low, 3)[index+1] &&
			High[index]  - Low[index] > ATR(atrPeriod)[1] * 2
			) {
				return true;
			}
			
			return false;
		}
		
		// isInsideDnBar
		//
		private bool isInsideDnBar(int indexOne, int indexTwo)
		{
			if(
			High[indexOne] <= High[indexTwo] &&
			Math.Min(Open[indexOne], Close[indexOne]) >= Low[indexTwo]
			) {
				return true;
			}
			
			return false;
		}
		
		// isInsideDnBreakoutBar
		//
		private bool isInsideDnBreakoutBar(int indexOne, int indexTwo)
		{
			if(
			High[indexOne]  <= High[indexTwo] &&
			Close[indexOne] <  MIN(Low, indexTwo-indexOne)[1] &&
			Low[indexOne]   <  MIN(Low, indexTwo-indexOne)[1]
			) {
				return true;
			}
			
			return false;
		}
		
		#endregion
		
		#region Demand
		
		// checkDemand
		//
		private void checkDemand()
		{
			// Regular
			
			if(currLoVal != prevLoVal)
			{
				if(MIN(Low, currLoBar)[0] >= currLoVal)
				{
					if(!activeDemandZoneExists(currLoVal) && isValidDemandZone(currHiVal, currLoVal))
					{
						br = High[currHiBar] - Low[currHiBar];
						zr = Math.Max(Open[currLoBar], Close[currLoBar]) - Low[currHiBar];
						zl = currLoVal;
						zh = (zr > atr) ? Math.Min(Open[currLoBar], Close[currLoBar]) : Math.Max(Open[currLoBar], Close[currLoBar]);
						zh = (zh - zl > atr) ? (zl + atr) : zh;
						zh = (br < atr * 0.75) ? High[currHiBar] : zh;
						zh = (Math.Abs(zh - zl) < minTicks * TickSize) ? zl + minTicks * TickSize : zh;
						zb = CurrentBar - currLoBar;
						zt = "d";
						zc = "r";
						za = true;
						
						Zones.Add(new Zone(zl, zh, zb, zt, zc, za));
					}
				}
			}
			
			// Continuation
			
			con = isUpContinuation();
			
			if(con != -1)
			{
				currHiCon = MAX(High, con)[1];
				currLoCon = MIN(Low, con)[0];
				
				if(currHiCon - currLoCon <= atr)
				{
					if(!activeDemandZoneExists(currLoCon) && isValidDemandZone(currHiCon, currLoCon))
					{
						zl = currLoCon;
						zh = currHiCon;
						zb = CurrentBar - (con);
						zt = "d";
						zc = "c";
						za = true;
						
						zh = (zh - zl < TickSize) ? (zl + TickSize) : zh;
						
						Zones.Add(new Zone(zl, zh, zb, zt, zc, za));
					}
				}
			}
		}
		
		// getNextDemandZone
		//
		private int getNextDemandZone(double price)
		{
			double max = double.MinValue;
			int    ind = -1;
			
			for(int i=0;i<Zones.Count;i++)
			{
				if(Zones[i].a == true && Zones[i].t == "d")
				{
					if(Zones[i].l < price && Zones[i].h > max)
					{
						ind = i;
					}
				}
			}
			
			return ind;
		}
		
		// activeDemandZoneExists
		//
		private bool activeDemandZoneExists(double lo)
		{
			bool exists = false;
			
			for(int i=0;i<Zones.Count;i++)
			{
				if(Zones[i].a == true && Zones[i].t == "d")
				{
					if(Zones[i].l == lo)
					{
						exists = true;
						break;
					}
				}
			}
			
			return exists;
		}
		
		// isValidDemandZone
		//
		private bool isValidDemandZone(double hi, double lo)
		{
			bool valid = true;
			
			for(int i=0;i<Zones.Count;i++)
			{
				if(Zones[i].a == true && Zones[i].t == "d")
				{
					if(
					(lo >= Zones[i].l && lo <= Zones[i].h) ||
					(hi >= Zones[i].l && hi <= Zones[i].h)
					) {
						valid = false;
						break;
					}
				}
			}
			
			return valid;
		}
		
		// isUpContinuation
		//
		private int isUpContinuation()
		{
			bool val = true;
			int  bar = -1;
			
			for(int i=10;i>=2;i--)
			{
				if(isUpMove(i))
				{
					val = true;
					
					for(int j=i-1;j>=1;j--)
					{
						if(!isInsideUpBar(j, i))
						{
							val = false;
							break;
						}
					}
					
					if(val)
					{
						val = false;
						
						for(int j=i;j>=1;j--)
						{
							if(Close[j] <= Open[j])
							{
								val = true;
								break;
							}
						}
					}
					
					if(val)
					{
						if(isInsideUpBreakoutBar(0, i))
						{
							bar = i;
							break;
						}
					}
				}
			}
			
			return bar;
		}
		
		// isUpMove
		//
		private bool isUpMove(int index)
		{
			if(
			Close[index]   > KeltnerChannel(1.0, 10).Upper[index]   ||
			Close[index+1] > KeltnerChannel(1.0, 10).Upper[index+1] ||
			Close[index+2] > KeltnerChannel(1.0, 10).Upper[index+2]
			) {
				if(
				isUpBar(index) &&
				isUpBar(index+1) &&
				isUpBar(index+2)
				) {
					return true;
				}
				
				if(
				isStrongUpBar(index)
				) {
					return true;
				}
			}
			
			return false;
		}
		
		// isUpBar
		//
		private bool isUpBar(int index)
		{
			if(
			Close[index] > Open[index] &&
			Close[index] > Close[index+1] &&
			High[index]  > High[index+1] &&
			Low[index]   > Low[index+1]
			) {
				return true;
			}
			
			return false;
		}
		
		// isStrongUpBar
		//
		private bool isStrongUpBar(int index)
		{
			if(
			Close[index] > Open[index] &&
			Close[index] > Close[index+1] &&
			High[index]  > High[index+1] &&
			Low[index]   > Low[index+1] &&
			High[index]  > MAX(High, 3)[index+1] &&
			High[index]  - Low[index] > ATR(atrPeriod)[1]
			) {
				return true;
			}
			
			if(
			Close[index] > Open[index] &&
			Close[index] > Close[index+1] &&
			Close[index] > MAX(High, 3)[index+1] &&
			High[index]  - Low[index] > ATR(atrPeriod)[1] * 2
			) {
				return true;
			}
			
			return false;
		}
		
		// isInsideUpBar
		//
		private bool isInsideUpBar(int indexOne, int indexTwo)
		{
			if(
			Low[indexOne] >= Low[indexTwo] &&
			Math.Max(Open[indexOne], Close[indexOne]) <= High[indexTwo]
			) {
				return true;
			}
			
			return false;
		}
		
		// isInsideUpBreakoutBar
		//
		private bool isInsideUpBreakoutBar(int indexOne, int indexTwo)
		{
			if(
			Low[indexOne]   >= Low[indexTwo] &&
			Close[indexOne] >  MAX(High, indexTwo-indexOne)[1] &&
			High[indexOne]  >  MAX(High, indexTwo-indexOne)[1]
			) {
				return true;
			}
			
			return false;
		}
		
		#endregion
		
		#region updateZones
		
		// updateZones
		//
		private void updateZones()
		{
			for(int i=Zones.Count-1;i>=0;i--)
			{
				string n = Zones[i].n;
				
				if(Zones[i].a == true)
				{
					if(Zones[i].t == "s")
					{
						if(High[0] > Zones[i].h)
						{
							RemoveDrawObject(n);
							Zones.RemoveAt(i);
							continue;
						}
					}
					
					if(Zones[i].t == "d")
					{
						if(Low[0] < Zones[i].l)
						{
							RemoveDrawObject(n);
							Zones.RemoveAt(i);
							continue;
						}
					}
				}
			}
			
		}
		
		#endregion
		
		#region formatPrice
		
		// formatPrice
		//
		public string formatPrice(double price)
		{
			return Instrument.MasterInstrument.FormatPrice(Instrument.MasterInstrument.RoundToTickSize(price));
		}
		
		#endregion
		
		#region Poperties
		
		[NinjaScriptProperty]
		[Display(Name = "Supply Template Name", GroupName = "1.0 Zone Parameters", Order = 0)]
		public string supplyZoneTemplate
		{ get; set; }
		
		// ---
		
		[NinjaScriptProperty]
		[Display(Name = "Demand Template Name", GroupName = "1.0 Zone Parameters", Order = 1)]
		public string demandZoneTemplate
		{ get; set; }
		
		// ---
		
		[NinjaScriptProperty]
		[Display(Name = "Draw Global", GroupName = "1.0 Zone Parameters", Order = 7)]
		public bool drawGlobal
		{ get; set; }
		
		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private Infinity.SupDemZones[] cacheSupDemZones;
		public Infinity.SupDemZones SupDemZones(string supplyZoneTemplate, string demandZoneTemplate, bool drawGlobal)
		{
			return SupDemZones(Input, supplyZoneTemplate, demandZoneTemplate, drawGlobal);
		}

		public Infinity.SupDemZones SupDemZones(ISeries<double> input, string supplyZoneTemplate, string demandZoneTemplate, bool drawGlobal)
		{
			if (cacheSupDemZones != null)
				for (int idx = 0; idx < cacheSupDemZones.Length; idx++)
					if (cacheSupDemZones[idx] != null && cacheSupDemZones[idx].supplyZoneTemplate == supplyZoneTemplate && cacheSupDemZones[idx].demandZoneTemplate == demandZoneTemplate && cacheSupDemZones[idx].drawGlobal == drawGlobal && cacheSupDemZones[idx].EqualsInput(input))
						return cacheSupDemZones[idx];
			return CacheIndicator<Infinity.SupDemZones>(new Infinity.SupDemZones(){ supplyZoneTemplate = supplyZoneTemplate, demandZoneTemplate = demandZoneTemplate, drawGlobal = drawGlobal }, input, ref cacheSupDemZones);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.Infinity.SupDemZones SupDemZones(string supplyZoneTemplate, string demandZoneTemplate, bool drawGlobal)
		{
			return indicator.SupDemZones(Input, supplyZoneTemplate, demandZoneTemplate, drawGlobal);
		}

		public Indicators.Infinity.SupDemZones SupDemZones(ISeries<double> input , string supplyZoneTemplate, string demandZoneTemplate, bool drawGlobal)
		{
			return indicator.SupDemZones(input, supplyZoneTemplate, demandZoneTemplate, drawGlobal);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.Infinity.SupDemZones SupDemZones(string supplyZoneTemplate, string demandZoneTemplate, bool drawGlobal)
		{
			return indicator.SupDemZones(Input, supplyZoneTemplate, demandZoneTemplate, drawGlobal);
		}

		public Indicators.Infinity.SupDemZones SupDemZones(ISeries<double> input , string supplyZoneTemplate, string demandZoneTemplate, bool drawGlobal)
		{
			return indicator.SupDemZones(input, supplyZoneTemplate, demandZoneTemplate, drawGlobal);
		}
	}
}

#endregion
