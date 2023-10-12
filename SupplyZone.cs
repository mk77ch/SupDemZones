// 
// Copyright (C) 2023, NinjaTrader LLC <www.ninjatrader.com>.
// NinjaTrader reserves the right to modify or overwrite this NinjaScript component with each release.
//
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
		private				int							areaOpacity			= 15;
		private				Brush						areaBrush			= Brushes.Coral;
		private	readonly	DeviceBrush					areaBrushDevice		= new DeviceBrush();
		private	const		double						cursorSensitivity	= 15;
		private				ChartAnchor 				editingAnchor;
		private				bool						hasSetZOrder;
		private				bool						extendZone			= true;
		private				string 						labelText			= "M15";
		private 			Brush 						labelBrush			= Brushes.Coral;
		private	readonly	DeviceBrush					labelBrushDevice	= new DeviceBrush();
		private 			int							labelOpacity		= 30;
		private 			float						labelOffset			= 10f;

		public override bool SupportsAlerts { get { return true; } }

		public override IEnumerable<ChartAnchor> Anchors
		{
			get { return new[] { StartAnchor, EndAnchor }; }
		}

		// ---
		
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
				if (areaBrush != null)
				{
					if (areaBrush.IsFrozen)
						areaBrush = areaBrush.Clone();
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
		
		[XmlIgnore]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Label Color", GroupName = "NinjaScriptGeneral", Order = 7)]
		public Brush LabelBrush
		{
			get { return labelBrush; }
			set
			{
				labelBrush = value;
				if (labelBrush != null)
				{
					if (labelBrush.IsFrozen)
						labelBrush = labelBrush.Clone();
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
		[Display(ResourceType = typeof(Custom.Resource), Name = "Label Opacity", GroupName = "NinjaScriptGeneral", Order = 8)]
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
		
		[Range(0f, float.MaxValue)]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Label Offset", GroupName = "NinjaScriptGeneral", Order = 9)]
		public float LabelOffset
		{
			get { return labelOffset; }
			set
			{
				labelOffset = Math.Max(0, Math.Min(float.MaxValue, value));
			}
		}
		
		// ---
		
		[Display(ResourceType = typeof(Custom.Resource), Name = "Area Anchor", GroupName = "NinjaScriptGeneral", Order = 10)]
		public Stroke AnchorLineStroke { get; set; }
		
		// ---
		
		[Display(ResourceType = typeof(Custom.Resource), Name = "Extend Zone", GroupName = "NinjaScriptGeneral", Order = 11)]
		public bool ExtendZone { get; set; }
		
		// ---
		
		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			if (areaBrushDevice != null)
				areaBrushDevice.RenderTarget = null;
			if (labelBrushDevice != null)
				labelBrushDevice.RenderTarget = null;
		}

		public override IEnumerable<AlertConditionItem> GetAlertConditionItems()
		{
			yield return new AlertConditionItem 
			{
				Name					= "Supply Zone",
				ShouldOnlyDisplayName	= true,
			};
		}

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
					if (closest != null)
					{
						if (IsLocked)
							return Cursors.Arrow;
						return Cursors.SizeNS;
					}

					Point	endAnchorPixelPoint	= EndAnchor.GetPoint(chartControl, chartPanel, chartScale);
					Vector	totalVector			= endAnchorPixelPoint - startAnchorPixelPoint;
					if(MathHelper.IsPointAlongVector(point, startAnchorPixelPoint, totalVector, cursorSensitivity))
						return IsLocked ? Cursors.Arrow : Cursors.SizeAll;

					// check if cursor is along region edges
					foreach (Point anchorPoint in new[] {startAnchorPixelPoint, endAnchorPixelPoint})
					{
						if (Math.Abs(anchorPoint.Y - point.Y) <= cursorSensitivity)
							return IsLocked ? Cursors.Arrow : Cursors.SizeAll; 
					}
					return null;
			}
		}

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

		public override IEnumerable<Condition> GetValidAlertConditions()
		{
			return new[] { Condition.CrossInside, Condition.CrossOutside };
		}

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

		public override bool IsVisibleOnChart(ChartControl chartControl, ChartScale chartScale, DateTime firstTimeOnChart, DateTime lastTimeOnChart)
		{
			if (DrawingState == DrawingState.Building)
				return true;

			// check if active y range highlight is on scale or cross through
			if (Anchors.Any(a => a.Price <= chartScale.MaxValue && a.Price >= chartScale.MinValue))
				return true;
			return StartAnchor.Price <= chartScale.MinValue && EndAnchor.Price >= chartScale.MaxValue || EndAnchor.Price <= chartScale.MinValue && StartAnchor.Price >= chartScale.MaxValue;
		}

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

		public override void OnMouseDown(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, ChartAnchor dataPoint)
		{
			switch (DrawingState)
			{
				case DrawingState.Building:

					//dataPoint.Time = chartControl.FirstTimePainted.AddSeconds((chartControl.LastTimePainted - chartControl.FirstTimePainted).TotalSeconds / 2);

					if (StartAnchor.IsEditing)
					{
						dataPoint.CopyDataValues(StartAnchor);
						StartAnchor.IsEditing = false;
						dataPoint.CopyDataValues(EndAnchor);
					}
					else if (EndAnchor.IsEditing)
					{
						//dataPoint.Time		= StartAnchor.Time;
						//dataPoint.SlotIndex	= StartAnchor.SlotIndex;

						dataPoint.CopyDataValues(EndAnchor);
						EndAnchor.IsEditing = false;
					}
					if (!StartAnchor.IsEditing && !EndAnchor.IsEditing)
					{
						DrawingState	= DrawingState.Normal;
						IsSelected		= false;
					}
					break;
				case DrawingState.Normal:
					Point point = dataPoint.GetPoint(chartControl, chartPanel, chartScale);
					editingAnchor = GetClosestAnchor(chartControl, chartPanel, chartScale, cursorSensitivity, point);
					if (editingAnchor != null)
					{
						editingAnchor.IsEditing = true;
						DrawingState			= DrawingState.Editing;
					}
					else
					{
						if (GetCursor(chartControl, chartPanel, chartScale, point) == Cursors.SizeAll)
							DrawingState = DrawingState.Moving;
						else if (GetCursor(chartControl, chartPanel, chartScale, point) == Cursors.SizeWE || GetCursor(chartControl, chartPanel, chartScale, point) == Cursors.SizeNS)
							DrawingState = DrawingState.Editing;
						else if (GetCursor(chartControl, chartPanel, chartScale, point) == Cursors.Arrow)
							DrawingState = DrawingState.Editing;
						else if (GetCursor(chartControl, chartPanel, chartScale, point) == null)
							IsSelected = false;
					}
					break;
			}
		}

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

		public override void OnMouseUp(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, ChartAnchor dataPoint)
		{
			if (DrawingState == DrawingState.Building)
				return;

			DrawingState		= DrawingState.Normal;
			editingAnchor		= null;
		}

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				AnchorLineStroke		= new Stroke(Brushes.DarkGray, DashStyleHelper.Dash, 1f);
				AreaBrush				= Brushes.Coral;
				AreaOpacity				= 15;
				DrawingState			= DrawingState.Building;
				OutlineStroke			= new Stroke(Brushes.Coral, DashStyleHelper.Solid, 2f, 60);
				StartAnchor				= new ChartAnchor { DisplayName = Custom.Resource.NinjaScriptDrawingToolAnchorStart, IsEditing = true, DrawingTool = this };
				EndAnchor				= new ChartAnchor { DisplayName = Custom.Resource.NinjaScriptDrawingToolAnchorEnd, IsEditing = true, DrawingTool = this };
				ZOrderType				= DrawingToolZOrder.AlwaysDrawnFirst;
				ExtendZone				= true;
				LabelText				= "";
				LabelBrush				= Brushes.Coral;
				LabelOpacity			= 30;
				LabelOffset				= 10f;
			}
			else if (State == State.Terminated)
				Dispose();
		}

		public override void OnRender(ChartControl chartControl, ChartScale chartScale)
		{
			//Allow user to change ZOrder when manually drawn on chart
			if(!hasSetZOrder && !StartAnchor.IsNinjaScriptDrawn)
			{
				ZOrderType	= DrawingToolZOrder.Normal;
				ZOrder		= ChartPanel.ChartObjects.Min(z => z.ZOrder) - 1;
				hasSetZOrder = true;
			}
			RenderTarget.AntialiasMode	= SharpDX.Direct2D1.AntialiasMode.PerPrimitive;
			Stroke outlineStroke		= OutlineStroke;
			outlineStroke.RenderTarget	= RenderTarget;
			ChartPanel	chartPanel		= chartControl.ChartPanels[PanelIndex];

			// recenter region anchors to always be onscreen/centered
			/*
			double middleX				= chartPanel.X + chartPanel.W / 2d;
			double middleY				= chartPanel.Y + chartPanel.H / 2d;
			
			StartAnchor.UpdateXFromPoint(new Point(middleX, 0), chartControl, chartScale);
			EndAnchor.UpdateXFromPoint(new Point(middleX, 0), chartControl, chartScale);
			*/
			Point		startPoint	= StartAnchor.GetPoint(chartControl, chartPanel, chartScale);
			Point		endPoint	= EndAnchor.GetPoint(chartControl, chartPanel, chartScale);
			double		width		= endPoint.X - startPoint.X;
		
			AnchorLineStroke.RenderTarget = RenderTarget;

			if (!IsInHitTest && AreaBrush != null)
			{
				if (areaBrushDevice.Brush == null)
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
			
			if (!IsInHitTest && LabelBrush != null)
			{
				if (labelBrushDevice.Brush == null)
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

				// align to full pixel to avoid unneeded aliasing
				float strokePixAdjust = Math.Abs(outlineStroke.Width % 2d).ApproxCompare(0) == 0 ? 0.5f : 0f;
				
				float x = (float)Math.Min(startPoint.X, endPoint.X) + strokePixAdjust;
				float y = (float)Math.Min(startPoint.Y, endPoint.Y) + strokePixAdjust;
				float w = (ExtendZone) ? (float)(chartControl.CanvasRight - x) : (float)(chartControl.GetXByBarIndex(cb, cb.ToIndex) + barHalfWidth - x);
				float h = (float)Math.Abs(endPoint.Y - startPoint.Y);

				SharpDX.RectangleF rect = new SharpDX.RectangleF(x, y, w, h);
				
				if(!IsInHitTest && areaBrushDevice.BrushDX != null)
					RenderTarget.FillRectangle(rect, areaBrushDevice.BrushDX);

				SharpDX.Direct2D1.Brush tmpBrush = IsInHitTest ? chartControl.SelectionBrush : outlineStroke.BrushDX;
				SharpDX.Direct2D1.Brush labelBrush = LabelBrush.ToDxBrush(RenderTarget);
				
				RenderTarget.DrawLine(rect.TopLeft, rect.TopRight, outlineStroke.BrushDX, outlineStroke.Width, outlineStroke.StrokeStyle);
				RenderTarget.DrawLine(rect.BottomLeft, rect.BottomRight, outlineStroke.BrushDX, outlineStroke.Width, outlineStroke.StrokeStyle);
				
				if(LabelText != "" && rect.Width >= 5f && rect.Height >= 5f) 
				{
					NinjaTrader.Gui.Tools.SimpleFont sf = new NinjaTrader.Gui.Tools.SimpleFont("Arial", (int)(rect.Height/2)){ Bold = true };
					SharpDX.DirectWrite.TextFormat tf = sf.ToDirectWriteTextFormat();
					
					tf.TextAlignment = SharpDX.DirectWrite.TextAlignment.Trailing;
					tf.WordWrapping	 = SharpDX.DirectWrite.WordWrapping.NoWrap;
					
					SharpDX.DirectWrite.TextLayout tl = new SharpDX.DirectWrite.TextLayout(Core.Globals.DirectWriteFactory, LabelText, tf, rect.Width - LabelOffset, rect.Height);
					
					tl.ParagraphAlignment = SharpDX.DirectWrite.ParagraphAlignment.Center;
					
					if(!IsInHitTest && labelBrushDevice.BrushDX != null)
						RenderTarget.DrawTextLayout(rect.TopLeft, tl, labelBrushDevice.BrushDX, SharpDX.Direct2D1.DrawTextOptions.NoSnap);
					
					tf.Dispose();
					tl.Dispose();
				}
				
				if(IsSelected)
				{
					tmpBrush = IsInHitTest ? chartControl.SelectionBrush : AnchorLineStroke.BrushDX;
					RenderTarget.DrawLine(startPoint.ToVector2(), endPoint.ToVector2(), tmpBrush, AnchorLineStroke.Width, AnchorLineStroke.StrokeStyle);
				}
			}
		}
	}

	/// <summary>
	/// Represents an interface that exposes information regarding a Region Highlight Y IDrawingTool.
	/// </summary>
	[CLSCompliant(false)]
	public class SupplyZoneY : SupplyZone
	{
		public override object Icon { get { return Gui.Tools.Icons.DrawRegionHighlightY; } }

		protected override void OnStateChange()
		{
			base.OnStateChange();
			if (State == State.SetDefaults)
			{
				Name								= "Supply Zone";
				StartAnchor	.IsXPropertiesVisible	= false;
				EndAnchor	.IsXPropertiesVisible	= false;
			}
		}
	}
	
	public static partial class Draw
	{
		private static readonly Brush defaultSupplyZoneBrush	= Brushes.Coral;
		private const			int	  defaultSupplyZoneOpacity	= 15;

		private static T SupplyZoneCore<T>(
			NinjaScriptBase owner,
			string tag,
			bool isAutoScale,
			int startBarsAgo,
			DateTime startTime,
			double startY,
			int endBarsAgo,
			DateTime endTime,
			double endY,
			string labelText,
			Brush labelBrush,
			int labelOpacity,
			float labelOffset,
			bool extendZone,
			Brush brush,
			Brush areaBrush,
			int areaOpacity,
			bool isGlobal,
			string templateName
		) where T : SupplyZone
		{
			if (owner == null)
				throw new ArgumentException("owner");

			if (string.IsNullOrWhiteSpace(tag))
				throw new ArgumentException(@"tag cant be null or empty", "tag");

			if (isGlobal && tag[0] != GlobalDrawingToolManager.GlobalDrawingToolTagPrefix)
				tag = GlobalDrawingToolManager.GlobalDrawingToolTagPrefix + tag;

			T supplyZone = DrawingTool.GetByTagOrNew(owner, typeof(T), tag, templateName) as T;
			if (supplyZone == null)
				return null;

			DrawingTool.SetDrawingToolCommonValues(supplyZone, tag, isAutoScale, owner, isGlobal);

			ChartAnchor	startAnchor;
			ChartAnchor	endAnchor;

			// just create on current bar
			startAnchor = DrawingTool.CreateChartAnchor(owner, 0, owner.Time[0], startY);
			endAnchor	= DrawingTool.CreateChartAnchor(owner, 0, owner.Time[0], endY);

			startAnchor.CopyDataValues(supplyZone.StartAnchor);
			endAnchor.CopyDataValues(supplyZone.EndAnchor);
			
			// brushes can be null when using a templateName
			if (supplyZone.AreaBrush != null && areaBrush != null)
				supplyZone.AreaBrush = areaBrush.Clone();

			if (areaOpacity >= 0)
				supplyZone.AreaOpacity = areaOpacity;
			if (brush != null)
				supplyZone.OutlineStroke = new Stroke(brush);

			supplyZone.SetState(State.Active);
			return supplyZone;
		}

		/*
			NinjaScriptBase owner,
			string tag,
			bool isAutoScale,
			int startBarsAgo,
			DateTime startTime,
			double startY,
			int endBarsAgo,
			DateTime endTime,
			double endY,
			string labelText,
			Brush labelBrush,
			int labelOpacity,
			float labelOffset,
			bool extendZone,
			Brush brush,
			Brush areaBrush,
			int areaOpacity,
			bool isGlobal,
			string templateName
		*/
		
		/// <summary>
		/// Draws a region highlight y on a chart.
		/// </summary>
		/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
		/// <param name="tag">A user defined unique id used to reference the draw object</param>
		/// <param name="startY">The starting y value coordinate where the draw object will be drawn</param>
		/// <param name="endY">The end y value coordinate where the draw object will terminate</param>
		/// <param name="brush">The brush used to color draw object</param>
		/// <param name="extendZone">Determines if the draw object will be extended to the right canvas</param>
		/// <returns></returns>
		[CLSCompliant(false)]
		public static SupplyZoneY SupplyZoneY(NinjaScriptBase owner, string tag, double startY, double endY, Brush brush, bool extendZone)
		{
			return SupplyZoneCore<SupplyZoneY>(owner, tag, false, 0, Core.Globals.MinDate, startY, 0, Core.Globals.MinDate, endY, "", brush, 30, 10f, extendZone, brush, defaultSupplyZoneBrush, defaultSupplyZoneOpacity, false, null);
		}

		/// <summary>
		/// Draws a region highlight y on a chart.
		/// </summary>
		/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
		/// <param name="tag">A user defined unique id used to reference the draw object</param>
		/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
		/// <param name="startY">The starting y value coordinate where the draw object will be drawn</param>
		/// <param name="endY">The end y value coordinate where the draw object will terminate</param>
		/// <param name="brush">The brush used to color draw object</param>
		/// <param name="areaBrush">The brush used to color the fill region area of the draw object</param>
		/// <param name="areaOpacity"> Sets the level of transparency for the fill color. Valid values between 0 - 100. (0 = completely transparent, 100 = no opacity)</param>
		/// <param name="extendZone">Determines if the draw object will be extended to the right canvas</param>
		/// <returns></returns>
		[CLSCompliant(false)]
		public static SupplyZoneY SupplyZoneY(NinjaScriptBase owner, string tag, bool isAutoScale, double startY, double endY, string labelText, Brush labelBrush, int labelOpacity, float labelOffset, bool extendZone, Brush brush, Brush areaBrush, int areaOpacity)
		{
			return SupplyZoneCore<SupplyZoneY>(owner, tag, isAutoScale, 0, Core.Globals.MinDate, startY, 0, Core.Globals.MinDate, endY, labelText, labelBrush, labelOpacity, labelOffset, extendZone, brush, areaBrush, areaOpacity, false, null);
		}

		/// <summary>
		/// Draws a region highlight y on a chart.
		/// </summary>
		/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
		/// <param name="tag">A user defined unique id used to reference the draw object</param>
		/// <param name="startY">The starting y value coordinate where the draw object will be drawn</param>
		/// <param name="endY">The end y value coordinate where the draw object will terminate</param>
		/// <param name="extendZone">Determines if the draw object will be extended to the right canvas</param>
		/// <param name="isGlobal">Determines if the draw object will be global across all charts which match the instrument</param>
		/// <param name="templateName">The name of the drawing tool template the object will use to determine various visual properties</param>
		/// <returns></returns>
		[CLSCompliant(false)]
		public static SupplyZoneY SupplyZoneY(NinjaScriptBase owner, string tag, double startY, double endY, string labelText, Brush labelBrush, int labelOpacity, float labelOffset, bool extendZone, bool isGlobal, string templateName)
		{
			return SupplyZoneCore<SupplyZoneY>(owner, tag, false, 0, Core.Globals.MinDate, startY, 0, Core.Globals.MinDate, endY, labelText, labelBrush, labelOpacity, labelOffset, extendZone, null, null, -1, isGlobal, templateName);
		}
	}
}
