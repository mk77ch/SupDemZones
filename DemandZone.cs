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
	public class DemandZone : DrawingTool
	{
		#region Variables
		
		private			 bool		 logErrors		   = true;
		private			 int		 areaOpacity	   = 15;
		private			 Brush		 areaBrush		   = Brushes.LightGreen;
		private	readonly DeviceBrush areaBrushDevice   = new DeviceBrush();
		private	const	 double		 cursorSensitivity = 15;
		private			 ChartAnchor editingAnchor;
		private			 bool		 hasSetZOrder;
		private			 bool		 extendZone		   = true;
		private			 string 	 labelText		   = "M15";
		private			 int		 labelSize		   = 0;
		private 		 Brush 		 labelBrush		   = Brushes.LightGreen;
		private	readonly DeviceBrush labelBrushDevice  = new DeviceBrush();
		private 		 int		 labelOpacity	   = 30;
		private 		 int		 labelOffset	   = 0;

		public override bool SupportsAlerts { get { return true; } }

		#endregion
		
		#region OnStateChange
		
		protected override void OnStateChange()
		{
			if(State == State.SetDefaults)
			{
				DrawingState	 = DrawingState.Building;
				AnchorLineStroke = new Stroke(Brushes.DarkGray, DashStyleHelper.Dash, 1f);
				AreaBrush		 = Brushes.LightGreen;
				AreaOpacity		 = 15;
				OutlineStroke	 = new Stroke(Brushes.LightGreen, DashStyleHelper.Solid, 2f, 60);
				ExtendZone		 = false;
				LabelText		 = "";
				labelSize		 = 0;
				LabelBrush		 = Brushes.LightGreen;
				LabelOpacity	 = 30;
				LabelOffset		 = 0;
				
				StartAnchor = new ChartAnchor
				{
                    IsBrowsable = true,
                    IsEditing	= true,
					DrawingTool = this,
					DisplayName = Custom.Resource.NinjaScriptDrawingToolAnchorStart,
				};
				
				EndAnchor = new ChartAnchor
				{
                    IsBrowsable = true,
					IsEditing	= true,
					DrawingTool	= this,
					DisplayName = Custom.Resource.NinjaScriptDrawingToolAnchorEnd,
				};
				
				ZOrderType = DrawingToolZOrder.AlwaysDrawnFirst;
			}
			else if(State == State.Terminated)
			{
				Dispose();
			}
		}

		#endregion
		
		#region Icon
		
		public override object Icon
		{
			get {
				return  System.Windows.Media.Geometry.Parse("M24 32c13.3 0 24 10.7 24 24V408c0 13.3 10.7 24 24 24H488c13.3 0 24 10.7 24 24s-10.7 24-24 24H72c-39.8 0-72-32.2-72-72V56C0 42.7 10.7 32 24 32zM128 136c0-13.3 10.7-24 24-24l208 0c13.3 0 24 10.7 24 24s-10.7 24-24 24l-208 0c-13.3 0-24-10.7-24-24zm24 72H296c13.3 0 24 10.7 24 24s-10.7 24-24 24H152c-13.3 0-24-10.7-24-24s10.7-24 24-24zm0 96H424c13.3 0 24 10.7 24 24s-10.7 24-24 24H152c-13.3 0-24-10.7-24-24s10.7-24 24-24z");
			}
		}
		
		#endregion
		
		#region Properties
		
		public override IEnumerable<ChartAnchor> Anchors { get { return new[] { StartAnchor, EndAnchor }; } }
		
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
			set { LabelBrush = Serialize.StringToBrush(value); }
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
			try
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
			catch(Exception e)
			{
				if(logErrors)
				{
					Print(e.ToString());
				}
			}
		}
		
		#endregion
		
		#region GetAlertConditionItems

		public override IEnumerable<AlertConditionItem> GetAlertConditionItems()
		{
			yield return new AlertConditionItem 
			{
				Name					= "Demand Zone",
				ShouldOnlyDisplayName	= true,
			};
		}
		
		#endregion
		
		#region GetCursor

		public override Cursor GetCursor(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, Point point)
		{
			try
			{
				switch(DrawingState)
				{
					case DrawingState.Building	: return Cursors.Pen;
					case DrawingState.Editing	: return IsLocked ? Cursors.No : Cursors.SizeNS;
					case DrawingState.Moving	: return IsLocked ? Cursors.No : Cursors.SizeAll;
					default:
						
						Point		startAnchorPixelPoint = StartAnchor.GetPoint(chartControl, chartPanel, chartScale);
						ChartAnchor	closest				  = GetClosestAnchor(chartControl, chartPanel, chartScale, cursorSensitivity, point);
						
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
						{
							return IsLocked ? Cursors.Arrow : Cursors.SizeAll;
						}
						
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
			catch(Exception e)
			{
				if(logErrors)
				{
					Print(e.ToString());
				}
			}
			
			return null;
		}

		#endregion
		
		#region GetSelectionPoints
		
		public override Point[] GetSelectionPoints(ChartControl chartControl, ChartScale chartScale)
		{
			try
			{
				ChartPanel chartPanel = chartControl.ChartPanels[PanelIndex];
				Point	   startPoint = StartAnchor.GetPoint(chartControl, chartPanel, chartScale);
				Point	   endPoint	  = EndAnchor.GetPoint(chartControl, chartPanel, chartScale);
				
				double	   middleX	= chartPanel.X + chartPanel.W / 2;
				double	   middleY	= chartPanel.Y + chartPanel.H / 2;
				Point	   midPoint	= new Point((startPoint.X + endPoint.X) / 2, (startPoint.Y + endPoint.Y) / 2);
				
				return new[] { startPoint, endPoint };
			}
			catch(Exception e)
			{
				if(logErrors)
				{
					Print(e.ToString());
				}
			}
			
			return null;
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
			try
			{
				double	 minPrice = Anchors.Min(a => a.Price);
				double	 maxPrice = Anchors.Max(a => a.Price);
				DateTime minTime  = Anchors.Min(a => a.Time);
				
				Predicate<ChartAlertValue> predicate = v =>
				{
					bool   isInside  =  v.Value >= minPrice && v.Value <= maxPrice && v.Time >= minTime;
					return condition == Condition.CrossInside ? isInside : !isInside;
				};
				
				return MathHelper.DidPredicateCross(values, predicate);
			}
			catch(Exception e)
			{
				if(logErrors)
				{
					Print(e.ToString());
				}
			}
			
			return false;
		}

		#endregion
		
		#region IsVisibleOnChart
		
		public override bool IsVisibleOnChart(ChartControl chartControl, ChartScale chartScale, DateTime firstTimeOnChart, DateTime lastTimeOnChart)
		{
			try
			{
				if(DrawingState == DrawingState.Building)
				{
					return true;
				}
				
				return Anchors.Any(a => a.Price <= chartScale.MaxValue && a.Price >= chartScale.MinValue && lastTimeOnChart >= a.Time);
			}
			catch(Exception e)
			{
				if(logErrors)
				{
					Print(e.ToString());
				}
			}
			
			return false;
		}

		#endregion
		
		#region OnCalculateMinMax
		
		public override void OnCalculateMinMax()
		{
			try
			{
				MinValue = double.MaxValue;
				MaxValue = double.MinValue;

				if(!IsVisible)
				{
					return;
				}

				foreach(ChartAnchor anchor in Anchors)
				{
					MinValue = Math.Min(anchor.Price, MinValue);
					MaxValue = Math.Max(anchor.Price, MaxValue);
				}
			}
			catch(Exception e)
			{
				if(logErrors)
				{
					Print(e.ToString());
				}
			}
		}

		#endregion
		
		#region OnMouseDown
		
		public override void OnMouseDown(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, ChartAnchor dataPoint)
		{
			try
			{
				switch (DrawingState)
				{
					case DrawingState.Building:
						
						if(StartAnchor.IsEditing)
						{
							dataPoint.CopyDataValues(StartAnchor);
							StartAnchor.IsEditing = false;
							dataPoint.CopyDataValues(EndAnchor);
						}
						else if(EndAnchor.IsEditing)
						{
							dataPoint.CopyDataValues(EndAnchor);
							EndAnchor.IsEditing = false;
						}
						if(!StartAnchor.IsEditing && !EndAnchor.IsEditing)
						{
							DrawingState = DrawingState.Normal;
							IsSelected	 = false;
						}
						break;
						
					case DrawingState.Normal:
						
						Point point   = dataPoint.GetPoint(chartControl, chartPanel, chartScale);
						editingAnchor = GetClosestAnchor(chartControl, chartPanel, chartScale, cursorSensitivity, point);
						
						if(editingAnchor != null)
						{
							editingAnchor.IsEditing = true;
							DrawingState			= DrawingState.Editing;
						}
						else
						{
							if(GetCursor(chartControl, chartPanel, chartScale, point) == Cursors.SizeAll)
							{
								DrawingState = DrawingState.Moving;
							}
							else if(GetCursor(chartControl, chartPanel, chartScale, point) == Cursors.SizeWE || GetCursor(chartControl, chartPanel, chartScale, point) == Cursors.SizeNS)
							{
								DrawingState = DrawingState.Editing;
							}
							else if(GetCursor(chartControl, chartPanel, chartScale, point) == Cursors.Arrow)
							{
								DrawingState = DrawingState.Editing;
							}
							else if(GetCursor(chartControl, chartPanel, chartScale, point) == null)
							{
								IsSelected = false;
							}
						}
						break;
				}
			}
			catch(Exception e)
			{
				if(logErrors)
				{
					Print(e.ToString());
				}
			}
		}

		#endregion
		
		#region OnMouseMove
		
		public override void OnMouseMove(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, ChartAnchor dataPoint)
		{
			try
			{
				if(IsLocked && DrawingState != DrawingState.Building)
				{
					return;
				}
				if(DrawingState == DrawingState.Building && EndAnchor.IsEditing)
				{
					dataPoint.CopyDataValues(EndAnchor);
				}
				else if(DrawingState == DrawingState.Editing && editingAnchor != null)
				{
					dataPoint.CopyDataValues(editingAnchor);
				}
				else if(DrawingState == DrawingState.Moving)
				{
					foreach(ChartAnchor anchor in Anchors)
					{
						anchor.MoveAnchor(InitialMouseDownAnchor, dataPoint, chartControl, chartPanel, chartScale, this);
					}
				}
			}
			catch(Exception e)
			{
				if(logErrors)
				{
					Print(e.ToString());
				}
			}
		}

		#endregion
		
		#region OnMouseUp
		
		public override void OnMouseUp(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, ChartAnchor dataPoint)
		{
			try
			{
				if(DrawingState == DrawingState.Building)
				{
					return;
				}
				
				DrawingState  = DrawingState.Normal;
				editingAnchor = null;
			}
			catch(Exception e)
			{
				if(logErrors)
				{
					Print(e.ToString());
				}
			}
		}

		#endregion
		
		#region OnRender
		
		public override void OnRender(ChartControl chartControl, ChartScale chartScale)
		{
			try
			{
				ChartBars cb = GetAttachedToChartBars();
				
				if(cb == null)
				{
					return;
				}
				
				double	 minPrice = Anchors.Min(a => a.Price);
				double	 maxPrice = Anchors.Max(a => a.Price);
				DateTime minTime = Anchors.Min(a => a.Time);
				DateTime marTime = chartControl.GetTimeByX(chartControl.GetXByBarIndex(cb, cb.ToIndex));
				
				if((minPrice <= chartScale.MaxValue || maxPrice >= chartScale.MinValue) && marTime >= minTime)
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
							Brush brushCopy			= AreaBrush.Clone();
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
							Brush brushCopy			= LabelBrush.Clone();
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
			catch(Exception e)
			{
				if(logErrors)
				{
					Print(e.ToString());
				}
			}
		}
		
		#endregion
		
		#region getTextMetrics
		
		private Tuple<float, float> getTextMetrics(int fontSize, string text)
		{
			try
			{
				float textHeight = 0f;
				
				SharpDX.DirectWrite.TextLayout  tl = new SharpDX.DirectWrite.TextLayout(Core.Globals.DirectWriteFactory, text, new NinjaTrader.Gui.Tools.SimpleFont("Arial", fontSize){ Bold = true }.ToDirectWriteTextFormat(), ChartPanel.W, ChartPanel.H);
				SharpDX.DirectWrite.LineMetrics lm = tl.GetLineMetrics().FirstOrDefault();
				
				float yOffset = tl.Metrics.Height - lm.Baseline;
				
				return Tuple.Create(tl.Metrics.Width, lm.Baseline - yOffset);
			}
			catch(Exception e)
			{
				if(logErrors)
				{
					Print(e.ToString());
				}
			}
			
			return null;
		}
		
		#endregion
	}

	#region Draw
	
	public static partial class Draw
	{
		private static DemandZone DemandZoneCore(
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
			try
			{
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
				
				DemandZone demandZone = DrawingTool.GetByTagOrNew(owner, typeof(DemandZone), tag, templateName) as DemandZone;
				
				if(demandZone == null)
				{
					return null;
				}
				
				DrawingTool.SetDrawingToolCommonValues(demandZone, tag, isAutoScale, owner, isGlobal);
				
				ChartAnchor	startAnchor	= DrawingTool.CreateChartAnchor(owner, startBarsAgo, startTime, startY);
				ChartAnchor	endAnchor	= DrawingTool.CreateChartAnchor(owner, endBarsAgo, endTime, endY);
				
				startAnchor.CopyDataValues(demandZone.StartAnchor);
				endAnchor.CopyDataValues(demandZone.EndAnchor);
				
				//
				// Area
				//
				
				if(areaBrush != null)
				{
					demandZone.AreaBrush = areaBrush.Clone();
				}
				
				if(areaOpacity >= 0)
				{
					demandZone.AreaOpacity = areaOpacity;
				}
				
				//
				// Line
				//
				
				if(lineBrush != null)
				{
					demandZone.OutlineStroke = new Stroke(lineBrush.Clone());
					
					if(lineStyle != null)
					{
						demandZone.OutlineStroke.DashStyleHelper = lineStyle;
					}
				}
				
				if(lineWidth >= 0)
				{
					demandZone.OutlineStroke.Width = (float)lineWidth;
				}
				
				if(lineOpacity >= 0)
				{
					demandZone.OutlineStroke.Opacity = Math.Max(0, Math.Min(100, lineOpacity));
				}
				
				//
				// Label
				//
				
				if(labelText != "")
				{
					demandZone.LabelText = labelText;
				}
				
				if(labelSize >= 0)
				{
					demandZone.LabelSize = labelSize;
				}
				
				if(labelBrush != null)
				{
					demandZone.LabelBrush = labelBrush.Clone();
				}
				
				if(labelOpacity >= 0)
				{
					demandZone.LabelOpacity = labelOpacity;
				}
				
				if(labelOffset >= 0)
				{
					demandZone.LabelOffset = labelOffset;
				}
				
				//
				// Extend
				//
				
				if(extendZone != null)
				{
					demandZone.ExtendZone = extendZone;
				}
				
				demandZone.SetState(State.Active);
				
				return demandZone;
			}
			catch(Exception e)
			{}
			
			return null;
		}
		
		/// <summary>
		/// Draws a demand zone.
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
		public static DemandZone DemandZone(
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
			return DemandZoneCore(
				owner, tag, isAutoScale, startBarsAgo, Core.Globals.MinDate, startY, endBarsAgo, Core.Globals.MinDate, endY,
				areaBrush, areaOpacity, 
				lineBrush, lineWidth, lineOpacity, lineStyle,
				labelText, labelSize, labelBrush, labelOpacity, labelOffset,
				extendZone, isGlobal, templateName
			);
		}
		
		/// <summary>
		/// Draws a demand zone.
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
		public static DemandZone DemandZone(
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
			return DemandZoneCore(
				owner, tag, isAutoScale, int.MinValue, startTime, startY, int.MinValue, endTime, endY,
				areaBrush, areaOpacity, 
				lineBrush, lineWidth, lineOpacity, lineStyle,
				labelText, labelSize, labelBrush, labelOpacity, labelOffset,
				extendZone, isGlobal, templateName
			);
		}
		
		/// <summary>
		/// Draws a demand zone.
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
		public static DemandZone DemandZone(
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
			return DemandZoneCore(
				owner, tag, isAutoScale, startBarsAgo, Core.Globals.MinDate, startY, endBarsAgo, Core.Globals.MinDate, endY,
				null, -1, 
				null, -1, -1, DashStyleHelper.Solid,
				labelText, -1, null, -1, labelOffset,
				extendZone, isGlobal, templateName
			);
		}
		
		/// <summary>
		/// Draws a demand zone.
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
		public static DemandZone DemandZone(
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
			return DemandZoneCore(
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
