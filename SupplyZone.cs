#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
#endregion

namespace NinjaTrader.NinjaScript.DrawingTools
{
	[CLSCompliant(false)]
	public abstract class SupplyZone : DrawingTool
	{
		#region Variables
		
		private				int							areaOpacity			= 15;
		private				Brush						areaBrush			= Brushes.Coral;
		private	readonly	DeviceBrush					areaBrushDevice		= new DeviceBrush();
		private	const		double						cursorSensitivity	= 15;
		private				ChartAnchor 				editingAnchor;
		private				bool						hasSetZOrder;
		private				bool						extendZone			= true;
		private				string 						labelText			= "M15";
		private				int							labelSize			= 0;
		private 			Brush 						labelBrush			= Brushes.Coral;
		private	readonly	DeviceBrush					labelBrushDevice	= new DeviceBrush();
		private 			int							labelOpacity		= 30;
		private 			int							labelOffset			= 0;

		public override bool SupportsAlerts { get { return true; } }

		#endregion

		#region Anchors
		
		public override IEnumerable<ChartAnchor> Anchors
		{
			get { return new[] { StartAnchor, EndAnchor }; }
		}
		
		#endregion
		
		#region Properties
		
		[Display(Order = 1)]
		public ChartAnchor StartAnchor { get; set; }
		
		// ---
		
		[Display(Order = 2)]
		public ChartAnchor EndAnchor { get; set; }
		
		// ---
		
		[XmlIgnore]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Area Color", GroupName = "NinjaScriptGeneral", Order = 3)]
		public Brush AreaBrush
		{
			get { return areaBrush; }
			set
			{
				areaBrush = value;
				if(areaBrush != null)
				{
					if(areaBrush.IsFrozen)
					{
						areaBrush = areaBrush.Clone();
					}
					areaBrush.Opacity = areaOpacity / 100d;
					areaBrush.Freeze();
				}
				areaBrushDevice.Brush = null;
			}
		}

		[Browsable(false)]
		public string AreaBrushSerialize
		{
			get { return Serialize.BrushToString(AreaBrush); }
			set { AreaBrush = Serialize.StringToBrush(value); }
		}

		// ---
		
		[Range(0, 100)]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Area Opacity", GroupName = "NinjaScriptGeneral", Order = 4)]
		public int AreaOpacity
		{
			get { return areaOpacity; }
			set
			{
				areaOpacity = Math.Max(0, Math.Min(100, value));
				areaBrushDevice.Brush = null;
			}
		}
		
		// ---
		
		[Display(ResourceType = typeof(Custom.Resource), Name = "Area Outline", GroupName = "NinjaScriptGeneral", Order = 5)]
		public Stroke OutlineStroke { get; set; }
		
		// ---
		
		[Display(ResourceType = typeof(Custom.Resource), Name = "Label Text", GroupName = "NinjaScriptGeneral", Order = 6)]
		public string LabelText { get; set; }
		
		// ---
		
		[Range(0, int.MaxValue)]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Label Size", GroupName = "NinjaScriptGeneral", Order = 7)]
		public int LabelSize
		{
			get { return labelSize; }
			set
			{
				labelSize = Math.Max(0, Math.Min(int.MaxValue, value));
			}
		}
		
		// ---
		
		[XmlIgnore]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Label Color", GroupName = "NinjaScriptGeneral", Order = 8)]
		public Brush LabelBrush
		{
			get { return labelBrush; }
			set
			{
				labelBrush = value;
				if(labelBrush != null)
				{
					if(labelBrush.IsFrozen)
					{
						labelBrush = labelBrush.Clone();
					}
					labelBrush.Opacity = labelOpacity / 100d;
					labelBrush.Freeze();
				}
				labelBrushDevice.Brush = null;
			}
		}

		[Browsable(false)]
		public string LabelBrushSerialize
		{
			get { return Serialize.BrushToString(LabelBrush); }
			set { labelBrush = Serialize.StringToBrush(value); }
		}
		
		// ---
		
		[Range(0, 100)]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Label Opacity", GroupName = "NinjaScriptGeneral", Order = 9)]
		public int LabelOpacity
		{
			get { return labelOpacity; }
			set
			{
				labelOpacity = Math.Max(0, Math.Min(100, value));
				labelBrushDevice.Brush = null;
			}
		}
		
		// ---
		
		[Range(0, int.MaxValue)]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Label Offset", GroupName = "NinjaScriptGeneral", Order = 10)]
		public int LabelOffset
		{
			get { return labelOffset; }
			set
			{
				labelOffset = Math.Max(0, Math.Min(int.MaxValue, value));
			}
		}
		
		// ---
		
		[Display(ResourceType = typeof(Custom.Resource), Name = "Area Anchor", GroupName = "NinjaScriptGeneral", Order = 11)]
		public Stroke AnchorLineStroke { get; set; }
		
		// ---
		
		[Display(ResourceType = typeof(Custom.Resource), Name = "Extend Zone", GroupName = "NinjaScriptGeneral", Order = 12)]
		public bool ExtendZone { get; set; }
		
		#endregion
		
		#region Dispose
		
		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			
			if(areaBrushDevice != null)
			{
				areaBrushDevice.RenderTarget = null;
			}
			if(labelBrushDevice != null)
			{
				labelBrushDevice.RenderTarget = null;
			}
		}
		
		#endregion
		
		#region GetAlertConditionItems

		public override IEnumerable<AlertConditionItem> GetAlertConditionItems()
		{
			yield return new AlertConditionItem 
			{
				Name					= "Supply Zone",
				ShouldOnlyDisplayName	= true,
			};
		}
		
		#endregion
		
		#region GetCursor

		public override Cursor GetCursor(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, Point point)
		{
			switch (DrawingState)
			{
				case DrawingState.Building	: return Cursors.Pen;
				case DrawingState.Editing	: return IsLocked ? Cursors.No : Cursors.SizeNS;
				case DrawingState.Moving	: return IsLocked ? Cursors.No : Cursors.SizeAll;
				default:
					Point		startAnchorPixelPoint	= StartAnchor.GetPoint(chartControl, chartPanel, chartScale);
					ChartAnchor	closest					= GetClosestAnchor(chartControl, chartPanel, chartScale, cursorSensitivity, point);
					if(closest != null)
					{
						if(IsLocked)
						{
							return Cursors.Arrow;
						}
						return Cursors.SizeNS;
					}

					Point	endAnchorPixelPoint	= EndAnchor.GetPoint(chartControl, chartPanel, chartScale);
					Vector	totalVector			= endAnchorPixelPoint - startAnchorPixelPoint;
					if(MathHelper.IsPointAlongVector(point, startAnchorPixelPoint, totalVector, cursorSensitivity))
						return IsLocked ? Cursors.Arrow : Cursors.SizeAll;

					// check if cursor is along zone edges
					foreach(Point anchorPoint in new[] {startAnchorPixelPoint, endAnchorPixelPoint})
					{
						if(Math.Abs(anchorPoint.Y - point.Y) <= cursorSensitivity)
						{
							return IsLocked ? Cursors.Arrow : Cursors.SizeAll;
						}
					}
					return null;
			}
		}

		#endregion
		
		#region GetSelectionPoints
		
		public override Point[] GetSelectionPoints(ChartControl chartControl, ChartScale chartScale)
		{
			ChartPanel	chartPanel	= chartControl.ChartPanels[PanelIndex];
			Point		startPoint	= StartAnchor.GetPoint(chartControl, chartPanel, chartScale);
			Point		endPoint	= EndAnchor.GetPoint(chartControl, chartPanel, chartScale);

			double		middleX		= chartPanel.X + chartPanel.W / 2;
			double		middleY		= chartPanel.Y + chartPanel.H / 2;
			Point		midPoint	= new Point((startPoint.X + endPoint.X) / 2, (startPoint.Y + endPoint.Y) / 2);
			
			return new[] { startPoint, endPoint };//.Select(p => new Point(middleX, p.Y)).ToArray();
		}

		#endregion
		
		#region GetValidAlertConditions
		
		public override IEnumerable<Condition> GetValidAlertConditions()
		{
			return new[] { Condition.CrossInside, Condition.CrossOutside };
		}

		#endregion
		
		#region IsAlertConditionTrue
		
		public override bool IsAlertConditionTrue(AlertConditionItem conditionItem, Condition condition, ChartAlertValue[] values, ChartControl chartControl, ChartScale chartScale)
		{
			double		minPrice	= Anchors.Min(a => a.Price);
			double		maxPrice	= Anchors.Max(a => a.Price);
			DateTime	minTime		= Anchors.Min(a => a.Time);
			DateTime	maxTime		= Anchors.Max(a => a.Time);

			Predicate<ChartAlertValue> predicate = v =>
			{
				bool isInside = v.Value >= minPrice && v.Value <= maxPrice;
				return condition == Condition.CrossInside ? isInside : !isInside;
			};
			return MathHelper.DidPredicateCross(values, predicate);
		}

		#endregion
		
		#region IsVisibleOnChart
		
		public override bool IsVisibleOnChart(ChartControl chartControl, ChartScale chartScale, DateTime firstTimeOnChart, DateTime lastTimeOnChart)
		{
			if(DrawingState == DrawingState.Building)
			{
				return true;
			}

			// check if active y range highlight is on scale or cross through
			if(Anchors.Any(a => a.Price <= chartScale.MaxValue && a.Price >= chartScale.MinValue))
			{
				return true;
			}
			
			return StartAnchor.Price <= chartScale.MinValue && EndAnchor.Price >= chartScale.MaxValue || EndAnchor.Price <= chartScale.MinValue && StartAnchor.Price >= chartScale.MaxValue;
		}

		#endregion
		
		#region OnCalculateMinMax
		
		public override void OnCalculateMinMax()
		{
			MinValue = double.MaxValue;
			MaxValue = double.MinValue;

			if (!IsVisible)
				return;

				foreach (ChartAnchor anchor in Anchors)
				{
					MinValue = Math.Min(anchor.Price, MinValue);
					MaxValue = Math.Max(anchor.Price, MaxValue);
				}
		}

		#endregion
		
		#region OnMouseDown
		
		public override void OnMouseDown(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, ChartAnchor dataPoint)
		{
			switch (DrawingState)
			{
				case DrawingState.Building:

					//dataPoint.Time = chartControl.FirstTimePainted.AddSeconds((chartControl.LastTimePainted - chartControl.FirstTimePainted).TotalSeconds / 2);

					if(StartAnchor.IsEditing)
					{
						dataPoint.CopyDataValues(StartAnchor);
						StartAnchor.IsEditing = false;
						dataPoint.CopyDataValues(EndAnchor);
					}
					else if(EndAnchor.IsEditing)
					{
						//dataPoint.Time		= StartAnchor.Time;
						//dataPoint.SlotIndex	= StartAnchor.SlotIndex;

						dataPoint.CopyDataValues(EndAnchor);
						EndAnchor.IsEditing = false;
					}
					if(!StartAnchor.IsEditing && !EndAnchor.IsEditing)
					{
						DrawingState	= DrawingState.Normal;
						IsSelected		= false;
					}
					break;
				case DrawingState.Normal:
					Point point = dataPoint.GetPoint(chartControl, chartPanel, chartScale);
					editingAnchor = GetClosestAnchor(chartControl, chartPanel, chartScale, cursorSensitivity, point);
					if(editingAnchor != null)
					{
						editingAnchor.IsEditing = true;
						DrawingState			= DrawingState.Editing;
					}
					else
					{
						if(GetCursor(chartControl, chartPanel, chartScale, point) == Cursors.SizeAll)
							DrawingState = DrawingState.Moving;
						else if(GetCursor(chartControl, chartPanel, chartScale, point) == Cursors.SizeWE || GetCursor(chartControl, chartPanel, chartScale, point) == Cursors.SizeNS)
							DrawingState = DrawingState.Editing;
						else if(GetCursor(chartControl, chartPanel, chartScale, point) == Cursors.Arrow)
							DrawingState = DrawingState.Editing;
						else if(GetCursor(chartControl, chartPanel, chartScale, point) == null)
							IsSelected = false;
					}
					break;
			}
		}

		#endregion
		
		#region OnMouseMove
		
		public override void OnMouseMove(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, ChartAnchor dataPoint)
		{
			if (IsLocked && DrawingState != DrawingState.Building)
				return;
			if (DrawingState == DrawingState.Building && EndAnchor.IsEditing)
			{
				//dataPoint.Time = chartControl.FirstTimePainted.AddSeconds((chartControl.LastTimePainted - chartControl.FirstTimePainted).TotalSeconds / 2);
			
				dataPoint.CopyDataValues(EndAnchor);
			}
			else if (DrawingState == DrawingState.Editing && editingAnchor != null)
				dataPoint.CopyDataValues(editingAnchor);
			else if (DrawingState == DrawingState.Moving)
				foreach (ChartAnchor anchor in Anchors)
					anchor.MoveAnchor(InitialMouseDownAnchor, dataPoint, chartControl, chartPanel, chartScale, this);
		}

		#endregion
		
		#region OnMouseUp
		
		public override void OnMouseUp(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, ChartAnchor dataPoint)
		{
			if (DrawingState == DrawingState.Building)
				return;

			DrawingState		= DrawingState.Normal;
			editingAnchor		= null;
		}

		#endregion
		
		#region OnStateChange
		
		protected override void OnStateChange()
		{
			if(State == State.SetDefaults)
			{
				AnchorLineStroke	= new Stroke(Brushes.DarkGray, DashStyleHelper.Dash, 1f);
				AreaBrush			= Brushes.Coral;
				AreaOpacity			= 15;
				DrawingState		= DrawingState.Building;
				OutlineStroke		= new Stroke(Brushes.Coral, DashStyleHelper.Solid, 2f, 60);
				StartAnchor			= new ChartAnchor { DisplayName = Custom.Resource.NinjaScriptDrawingToolAnchorStart, IsEditing = true, DrawingTool = this };
				EndAnchor			= new ChartAnchor { DisplayName = Custom.Resource.NinjaScriptDrawingToolAnchorEnd, IsEditing = true, DrawingTool = this };
				ZOrderType			= DrawingToolZOrder.AlwaysDrawnFirst;
				ExtendZone			= false;
				LabelText			= "";
				labelSize			= 0;
				LabelBrush			= Brushes.Coral;
				LabelOpacity		= 30;
				LabelOffset			= 0;
			}
			else if(State == State.Terminated)
			{
				Dispose();
			}
		}

		#endregion
		
		#region OnRender
		
		public override void OnRender(ChartControl chartControl, ChartScale chartScale)
		{
			if(!hasSetZOrder && !StartAnchor.IsNinjaScriptDrawn)
			{
				ZOrderType	 = DrawingToolZOrder.Normal;
				ZOrder		 = ChartPanel.ChartObjects.Min(z => z.ZOrder) - 1;
				hasSetZOrder = true;
			}
			
			RenderTarget.AntialiasMode = SharpDX.Direct2D1.AntialiasMode.PerPrimitive;
			Stroke outlineStroke	   = OutlineStroke;
			outlineStroke.RenderTarget = RenderTarget;
			ChartPanel	chartPanel	   = chartControl.ChartPanels[PanelIndex];
		
			Point startPoint = StartAnchor.GetPoint(chartControl, chartPanel, chartScale);
			Point endPoint	 = EndAnchor.GetPoint(chartControl, chartPanel, chartScale);
		
			AnchorLineStroke.RenderTarget = RenderTarget;

			if(!IsInHitTest && AreaBrush != null)
			{
				if(areaBrushDevice.Brush == null)
				{
					Brush brushCopy			= areaBrush.Clone();
					brushCopy.Opacity		= areaOpacity / 100d; 
					areaBrushDevice.Brush	= brushCopy;
				}
				areaBrushDevice.RenderTarget = RenderTarget;
			}
			else
			{
				areaBrushDevice.RenderTarget = null;
				areaBrushDevice.Brush = null;
			}
			
			if(!IsInHitTest && LabelBrush != null)
			{
				if(labelBrushDevice.Brush == null)
				{
					Brush brushCopy			= labelBrush.Clone();
					brushCopy.Opacity		= labelOpacity / 100d; 
					labelBrushDevice.Brush	= brushCopy;
				}
				labelBrushDevice.RenderTarget = RenderTarget;
			}
			else
			{
				labelBrushDevice.RenderTarget = null;
				labelBrushDevice.Brush = null;
			}
			
			ChartBars cb = GetAttachedToChartBars();
			
			if(chartControl.GetXByBarIndex(cb, cb.ToIndex) >= Math.Min(startPoint.X, endPoint.X))
			{
				int barFullWidth = chartControl.GetBarPaintWidth(cb);
				int barHalfWidth = (int)Math.Round(barFullWidth / 2.0);

				float x = (float)Math.Min(startPoint.X, endPoint.X);
				float y = (float)Math.Min(startPoint.Y, endPoint.Y);
				float w = (ExtendZone) ? (float)(chartControl.CanvasRight - x) : (float)(chartControl.GetXByBarIndex(cb, cb.ToIndex) + barHalfWidth - x);
				float h = (float)Math.Abs(endPoint.Y - startPoint.Y);

				SharpDX.RectangleF rect = new SharpDX.RectangleF(x, y, w, h);
				
				if(!IsInHitTest && areaBrushDevice.BrushDX != null)
				{
					RenderTarget.FillRectangle(rect, areaBrushDevice.BrushDX);
				}

				SharpDX.Direct2D1.Brush tmpBrush = IsInHitTest ? chartControl.SelectionBrush : outlineStroke.BrushDX;
				SharpDX.Direct2D1.Brush labelBrush = LabelBrush.ToDxBrush(RenderTarget);
				
				RenderTarget.DrawLine(rect.TopLeft, rect.TopRight, outlineStroke.BrushDX, outlineStroke.Width, outlineStroke.StrokeStyle);
				RenderTarget.DrawLine(rect.BottomLeft, rect.BottomRight, outlineStroke.BrushDX, outlineStroke.Width, outlineStroke.StrokeStyle);
				
				if(LabelText != "") 
				{
					int   				fontSize    = (LabelSize == 0) ? (int)(rect.Height * 0.8f) : labelSize;
					Tuple<float, float> textMetrics = getTextMetrics(fontSize, LabelText);
					float 				rightMargin = (LabelSize == 0) ? (rect.Height - textMetrics.Item2) / 2f + (float)labelOffset : (float)labelOffset;
					
					if(rect.Width >= textMetrics.Item1 + rightMargin && rect.Height >= textMetrics.Item2)
					{
						NinjaTrader.Gui.Tools.SimpleFont sf = new NinjaTrader.Gui.Tools.SimpleFont("Arial", fontSize){ Bold = true };
						SharpDX.DirectWrite.TextFormat tf = sf.ToDirectWriteTextFormat();
						
						tf.TextAlignment = SharpDX.DirectWrite.TextAlignment.Trailing;
						tf.WordWrapping	 = SharpDX.DirectWrite.WordWrapping.NoWrap;
						
						SharpDX.DirectWrite.TextLayout tl = new SharpDX.DirectWrite.TextLayout(Core.Globals.DirectWriteFactory, LabelText, tf, rect.Width - rightMargin, rect.Height);
						
						tl.ParagraphAlignment = SharpDX.DirectWrite.ParagraphAlignment.Center;
						
						if(!IsInHitTest && labelBrushDevice.BrushDX != null)
						{
							RenderTarget.DrawTextLayout(rect.TopLeft, tl, labelBrushDevice.BrushDX, SharpDX.Direct2D1.DrawTextOptions.NoSnap);
						}
						
						tf.Dispose();
						tl.Dispose();
					}
				}
				
				if(IsSelected)
				{
					tmpBrush = IsInHitTest ? chartControl.SelectionBrush : AnchorLineStroke.BrushDX;
					RenderTarget.DrawLine(startPoint.ToVector2(), endPoint.ToVector2(), tmpBrush, AnchorLineStroke.Width, AnchorLineStroke.StrokeStyle);
				}
			}
		}
		
		#endregion
		
		#region getTextMetrics
		
		private Tuple<float, float> getTextMetrics(int fontSize, string text)
		{
			float textHeight = 0f;
			
			SharpDX.DirectWrite.TextLayout  tl = new SharpDX.DirectWrite.TextLayout(Core.Globals.DirectWriteFactory, text, new NinjaTrader.Gui.Tools.SimpleFont("Arial", fontSize){ Bold = true }.ToDirectWriteTextFormat(), ChartPanel.W, ChartPanel.H);
			SharpDX.DirectWrite.LineMetrics lm = tl.GetLineMetrics().FirstOrDefault();
			
			float yOffset = tl.Metrics.Height - lm.Baseline;
			
			return Tuple.Create(tl.Metrics.Width, lm.Baseline - yOffset);
		}
		
		#endregion;
	}

	#region Interface
	
	/// <summary>
	/// Represents an interface that exposes information regarding a Supply Zone IDrawingTool.
	/// </summary>
	[CLSCompliant(false)]
	public class SupplyZoneI : SupplyZone
	{
		public override object Icon { get { return Gui.Tools.Icons.DrawRegionHighlightY; } }

		protected override void OnStateChange()
		{
			base.OnStateChange();
			
			if(State == State.SetDefaults)
			{
				Name = "Supply Zone";
			}
		}
	}
	
	#endregion
	
	#region Draw
	
	public static partial class Draw
	{
		private static SupplyZone SupplyZoneCore(
			NinjaScriptBase owner,
			string tag,
			bool isAutoScale,
			int startBarsAgo,
			DateTime startTime,
			double startY,
			int endBarsAgo,
			DateTime endTime,
			double endY,
			Brush areaBrush,
			int areaOpacity,
			Brush lineBrush,
			int lineWidth,
			int lineOpacity,
			DashStyleHelper lineStyle,
			string labelText,
			int labelSize,
			Brush labelBrush,
			int labelOpacity,
			int labelOffset,
			bool extendZone,
			bool isGlobal,
			string templateName
		) {
			if(owner == null)
			{
				throw new ArgumentException("owner");
			}

			if(string.IsNullOrWhiteSpace(tag))
			{
				throw new ArgumentException(@"tag cant be null or empty", "tag");
			}

			if(isGlobal && tag[0] != GlobalDrawingToolManager.GlobalDrawingToolTagPrefix)
			{
				tag = GlobalDrawingToolManager.GlobalDrawingToolTagPrefix + tag;
			}

			SupplyZone supplyZone = DrawingTool.GetByTagOrNew(owner, typeof(SupplyZone), tag, templateName) as SupplyZone;
			
			if(supplyZone == null)
			{
				return null;
			}

			DrawingTool.SetDrawingToolCommonValues(supplyZone, tag, isAutoScale, owner, isGlobal);

			ChartAnchor	startAnchor	= DrawingTool.CreateChartAnchor(owner, startBarsAgo, startTime, startY);
			ChartAnchor	endAnchor	= DrawingTool.CreateChartAnchor(owner, endBarsAgo, endTime, endY);

			startAnchor.CopyDataValues(supplyZone.StartAnchor);
			endAnchor.CopyDataValues(supplyZone.EndAnchor);
			
			//
			// Area
			//
			
			if(areaBrush != null)
			{
				supplyZone.AreaBrush = areaBrush.Clone();
			}
			
			if(areaOpacity >= 0)
			{
				supplyZone.AreaOpacity = areaOpacity;
			}
			
			//
			// Line
			//
			
			if(lineBrush != null)
			{
				supplyZone.OutlineStroke = new Stroke(lineBrush.Clone());
				
				if(lineStyle != null)
				{
					supplyZone.OutlineStroke.DashStyleHelper = lineStyle;
				}
			}
			
			if(lineWidth >= 0)
			{
				supplyZone.OutlineStroke.Width = (float)lineWidth;
			}
			
			if(lineOpacity >= 0)
			{
				supplyZone.OutlineStroke.Opacity = Math.Max(0, Math.Min(100, lineOpacity));
			}
			
			//
			// Label
			//
			
			if(labelText != "")
			{
				supplyZone.LabelText = labelText;
			}
			
			if(labelSize >= 0)
			{
				supplyZone.LabelSize = labelSize;
			}
			
			if(labelBrush != null)
			{
				supplyZone.LabelBrush = labelBrush.Clone();
			}
			
			if(labelOpacity >= 0)
			{
				supplyZone.LabelOpacity = labelOpacity;
			}
			
			if(labelOffset >= 0)
			{
				supplyZone.LabelOffset = labelOffset;
			}
			
			//
			// Extend
			//
			
			if(extendZone != null)
			{
				supplyZone.ExtendZone = extendZone;
			}

			supplyZone.SetState(State.Active);
			
			return supplyZone;
		}
		
		/// <summary>
		/// Draws a supply zone.
		/// </summary>
		/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
		/// <param name="tag">A user defined unique id used to reference the draw object</param>
		/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
		/// <param name="startBarsAgo">The starting bar (x axis coordinate) where the draw object will be drawn. For example, a value of 10 would paint the draw object 10 bars back.</param>
		/// <param name="startY">The starting y value coordinate where the draw object will be drawn</param>
		/// <param name="endBarsAgo">The end bar (x axis coordinate) where the draw object will terminate</param>
		/// <param name="endY">The end y value coordinate where the draw object will terminate</param>
		/// <param name="areaBrush">The brush used to color draw object</param>
		/// <param name="areaOpacity">Sets the level of transparency for the fill color. Valid values between 0 - 100. (0 = completely transparent, 100 = no opacity)</param>
		/// <param name="lineBrush">The brush used to color the lines</param>
		/// <param name="lineOpacity">Sets the level of transparency for the line color. Valid values between 0 - 100. (0 = completely transparent, 100 = no opacity)</param>
		/// <param name="labelText">The text to diplay as zone label</param>
		/// <param name="labelSize">The text size used for the label. (0 = dynamic)</param>
		/// <param name="labelBrush">The brush used to color the label</param>
		/// <param name="labelOpacity">Sets the level of transparency for the label color. Valid values between 0 - 100. (0 = completely transparent, 100 = no opacity)</param>
		/// <param name="lineStyle">The dash style used for the lines of the object</param>
		/// <param name="labelOffset">The offset in pixels from the left, used to draw the label</param>
		/// <param name="extendZone">Extend the zone to the right canvas</param>
		/// <param name="isGlobal">Determines if the draw object will be global across all charts which match the instrument</param>
		/// <param name="templateName">The name of the drawing tool template the object will use to determine various visual properties</param>
		/// <returns></returns>
		[CLSCompliant(false)]
		public static SupplyZone SupplyZone(
			NinjaScriptBase owner,
			string tag,
			bool isAutoScale,
			int startBarsAgo,
			double startY,
			int endBarsAgo,
			double endY,
			Brush areaBrush,
			int areaOpacity,
			Brush lineBrush,
			int lineWidth,
			int lineOpacity,
			DashStyleHelper lineStyle,
			string labelText,
			int labelSize,
			Brush labelBrush,
			int labelOpacity,
			int labelOffset,
			bool extendZone,
			bool isGlobal,
			string templateName
		) {
			return SupplyZoneCore(
				owner, tag, isAutoScale, startBarsAgo, Core.Globals.MinDate, startY, endBarsAgo, Core.Globals.MinDate, endY,
				areaBrush, areaOpacity, 
				lineBrush, lineWidth, lineOpacity, lineStyle,
				labelText, labelSize, labelBrush, labelOpacity, labelOffset,
				extendZone, isGlobal, templateName
			);
		}
		
		/// <summary>
		/// Draws a supply zone.
		/// </summary>
		/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
		/// <param name="tag">A user defined unique id used to reference the draw object</param>
		/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
		/// <param name="startTime">The starting time where the draw object will be drawn.</param>
		/// <param name="startY">The starting y value coordinate where the draw object will be drawn</param>
		/// <param name="endTime">The end time where the draw object will terminate</param>
		/// <param name="endY">The end y value coordinate where the draw object will terminate</param>
		/// <param name="areaBrush">The brush used to color draw object</param>
		/// <param name="areaOpacity">Sets the level of transparency for the fill color. Valid values between 0 - 100. (0 = completely transparent, 100 = no opacity)</param>
		/// <param name="lineBrush">The brush used to color the lines</param>
		/// <param name="lineOpacity">Sets the level of transparency for the line color. Valid values between 0 - 100. (0 = completely transparent, 100 = no opacity)</param>
		/// <param name="lineStyle">The dash style used for the lines of the object</param>
		/// <param name="labelText">The text to diplay as zone label</param>
		/// <param name="labelSize">The text size used for the label. (0 = dynamic)</param>
		/// <param name="labelBrush">The brush used to color the label</param>
		/// <param name="labelOpacity">Sets the level of transparency for the label color. Valid values between 0 - 100. (0 = completely transparent, 100 = no opacity)</param>
		/// <param name="labelOffset">The offset in pixels from the left, used to draw the label</param>
		/// <param name="extendZone">Extend the zone to the right canvas</param>
		/// <param name="isGlobal">Determines if the draw object will be global across all charts which match the instrument</param>
		/// <param name="templateName">The name of the drawing tool template the object will use to determine various visual properties</param>
		/// <returns></returns>
		[CLSCompliant(false)]
		public static SupplyZone SupplyZone(
			NinjaScriptBase owner,
			string tag,
			bool isAutoScale,
			DateTime startTime,
			double startY, 
			DateTime endTime,
			double endY,
			Brush areaBrush,
			int areaOpacity,
			Brush lineBrush,
			int lineWidth,
			int lineOpacity,
			DashStyleHelper lineStyle,
			string labelText,
			int labelSize,
			Brush labelBrush,
			int labelOpacity,
			int labelOffset,
			bool extendZone,
			bool isGlobal,
			string templateName
		) {
			return SupplyZoneCore(
				owner, tag, isAutoScale, int.MinValue, startTime, startY, int.MinValue, endTime, endY,
				areaBrush, areaOpacity, 
				lineBrush, lineWidth, lineOpacity, lineStyle,
				labelText, labelSize, labelBrush, labelOpacity, labelOffset,
				extendZone, isGlobal, templateName
			);
		}
		
		/// <summary>
		/// Draws a supply zone.
		/// </summary>
		/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
		/// <param name="tag">A user defined unique id used to reference the draw object</param>
		/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
		/// <param name="startBarsAgo">The starting bar (x axis coordinate) where the draw object will be drawn. For example, a value of 10 would paint the draw object 10 bars back.</param>
		/// <param name="startY">The starting y value coordinate where the draw object will be drawn</param>
		/// <param name="endBarsAgo">The end bar (x axis coordinate) where the draw object will terminate</param>
		/// <param name="endY">The end y value coordinate where the draw object will terminate</param>
		/// <param name="labelText">The text to diplay as zone label</param>
		/// <param name="labelOffset">The offset in pixels from the left, used to draw the label</param>
		/// <param name="extendZone">Extend the zone to the right canvas</param>
		/// <param name="isGlobal">Determines if the draw object will be global across all charts which match the instrument</param>
		/// <param name="templateName">The name of the drawing tool template the object will use to determine various visual properties</param>
		/// <returns></returns>
		[CLSCompliant(false)]
		public static SupplyZone SupplyZone(
			NinjaScriptBase owner,
			string tag,
			bool isAutoScale,
			int startBarsAgo,
			double startY,
			int endBarsAgo,
			double endY,
			string labelText,
			int labelOffset,
			bool extendZone,
			bool isGlobal,
			string templateName
		) {
			return SupplyZoneCore(
				owner, tag, isAutoScale, startBarsAgo, Core.Globals.MinDate, startY, endBarsAgo, Core.Globals.MinDate, endY,
				null, -1, 
				null, -1, -1, DashStyleHelper.Solid,
				labelText, -1, null, -1, labelOffset,
				extendZone, isGlobal, templateName
			);
		}
		
		/// <summary>
		/// Draws a supply zone.
		/// </summary>
		/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
		/// <param name="tag">A user defined unique id used to reference the draw object</param>
		/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
		/// <param name="startTime">The starting time where the draw object will be drawn.</param>
		/// <param name="startY">The starting y value coordinate where the draw object will be drawn</param>
		/// <param name="endTime">The end time where the draw object will terminate</param>
		/// <param name="endY">The end y value coordinate where the draw object will terminate</param>
		/// <param name="labelText">The text to diplay as zone label</param>
		/// <param name="labelOffset">The offset in pixels from the left, used to draw the label</param>
		/// <param name="extendZone">Extend the zone to the right canvas</param>
		/// <param name="isGlobal">Determines if the draw object will be global across all charts which match the instrument</param>
		/// <param name="templateName">The name of the drawing tool template the object will use to determine various visual properties</param>
		/// <returns></returns>
		[CLSCompliant(false)]
		public static SupplyZone SupplyZone(
			NinjaScriptBase owner,
			string tag,
			bool isAutoScale,
			DateTime startTime,
			double startY, 
			DateTime endTime,
			double endY,
			string labelText,
			int labelOffset,
			bool extendZone,
			bool isGlobal,
			string templateName
		) {
			return SupplyZoneCore(
				owner, tag, isAutoScale, int.MinValue, startTime, startY, int.MinValue, endTime, endY,
				null, -1, 
				null, -1, -1, DashStyleHelper.Solid,
				labelText, -1, null, -1, labelOffset,
				extendZone, isGlobal, templateName
			);
		}
	}
	
	#endregion
}
