﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Xml;
using System.Xml.Linq;
using Flee.PublicTypes;
using static ScreenDesigner.ColorOperations;

namespace ScreenDesigner
{
	public static class ColorOperations
	{
		public enum BpC    // bits per color
		{
			// 8-bit color bit lengths
			R8 = 3,
			G8 = 3,
			B8 = 2,

			// 16-bit color bit lengths
			R16 = 5,
			G16 = 6,
			B16 = 5
		}

		public static byte LimitColor(int color, BpC bitsPerColor)
		{
			int colorCur;
			int bits = (int)bitsPerColor;

			// Only keep the top bits in the byte
			color &= (int)(0xFF00 >> bits) & 0xFF;

			// Repeat upper color bits through lower bits
			for (colorCur = color; ; )
			{
				colorCur >>= bits;
				if (colorCur == 0)
					break;
				color |= colorCur;
			}
			return (byte)color;
		}
	}
	
	public class TextSize
	{
		TextBlock m_TextBlock;

		public TextSize(TextBlock text)
		{
			m_TextBlock = text;
		}

		public int Height
		{
			get
			{
				Measure();
				return (int)(m_TextBlock.DesiredSize.Height + 0.9);
			}
		}

		public int Width
		{
			get
			{
				Measure();
				return (int)(m_TextBlock.DesiredSize.Width + 0.9);
			}
		}

		void Measure()
		{
			if (!m_TextBlock.IsMeasureValid)
				m_TextBlock.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
		}
	}

	class XmlScreen
	{
		public const string StrColorDepth8 = "Color8bpp";
		public const string StrColorDepth16 = "Color16bpp";
		public const string StrColorDepth24 = "Color24bpp";

		const int IntNoValue = int.MinValue;

		#region Types

		class XmlGraphic
		{
			string m_xmlClone;
			int m_Height;
			protected int m_Width;

			public virtual int Height
			{
				get { return Visual == null ? m_Height : (int)Visual.Height; }
				set { if (Visual == null) m_Height = value; else Visual.Height = value; }
			}

			public virtual int Width
			{
				get { return Visual == null ? m_Width : (int)Visual.Width; }
				set { if (Visual == null) m_Width = value; else Visual.Width = value; }
			}

			public virtual string Content
			{
				set { }
			}

			public string Location { get; set; }

			public FrameworkElement Visual { get; set; }
			public virtual Element Owner { get; set; }

			public void SetAttribute(string Attr, string Value)
			{
				PropertyInfo prop;
				object obj;

				prop = GetType().GetProperty(Attr);
				if (prop != null)
					obj = this;
				else
				{
					prop = Visual?.GetType().GetProperty(Attr);
					obj = Visual;
				}

				if (prop != null)
				{
					if (prop.PropertyType == typeof(string))
						prop.SetValue(obj, Value);
					else if (prop.PropertyType == typeof(Brush))
						prop.SetValue(obj, new SolidColorBrush(ColorFromString(Value)));
					else
						prop.SetValue(obj, Convert.ChangeType(Element.EvalInt(Value), prop.PropertyType));

					m_xmlClone = null;  // invalide cached copy
				}
				else
					throw new Exception($"Unknown attribute name '{Attr}'");
			}

			public virtual void Draw(DrawResults DrawList, int x, int y)
			{
				if (!string.IsNullOrEmpty(Location))
					DrawList.Locations.Add(new Location(Location, x, y));

				if (Visual != null)
				{
					Visual.Arrange(new Rect(x, y, Width, Height));
					DrawList.Visual.Children.Add(Visual);
				}
			}

			public virtual void ElementComplete() { }

			public virtual XmlGraphic Clone()
			{
				XmlGraphic graphic;

				graphic = (XmlGraphic)MemberwiseClone();
				if (Visual != null)
				{
					if (m_xmlClone == null)
						m_xmlClone = XamlWriter.Save(Visual);
					graphic.Visual = (FrameworkElement)XamlReader.Load(new XmlTextReader(new StringReader(m_xmlClone)));
				}
				return graphic;
			}

			protected static Color ColorFromString(string color)
			{
				int val;
				PropertyInfo prop;

				if (string.IsNullOrEmpty(color))
					throw new Exception("Color is blank.");

				if (color[0] >= '0' && color[0] <= '9')
				{
					try
					{
						val = (int)new Int32Converter().ConvertFromString(color);
						return Color.FromRgb((byte)(val >> 16), (byte)((val >> 8) & 0xff), (byte)(val & 0xff));
					}
					catch (Exception)
					{}
					throw new Exception($"Invalid color value: {color}");
				}

				// Try looking it up by name
				// First look in our local colors
				if (Colors.TryGetValue(color, out val))
					return Color.FromRgb((byte)(val >> 16), (byte)((val >> 8) & 0xff), (byte)(val & 0xff));

				prop = typeof(Colors).GetProperty(color);
				if (prop == null)
					throw new Exception($"Invalid color name: {color}");
				return (Color)prop.GetValue(null);
			}
		}

		class XmlArea : XmlGraphic
		{
			public string Area { get; set; }
			public string HotSpot { get; set; }
			public string Group { get; set; }

			public override void Draw(DrawResults DrawList, int x, int y)
			{
				if (!string.IsNullOrEmpty(HotSpot))
					DrawList.HotSpots.Add(new HotSpot(HotSpot, Group, x, y, Width, Height));

				if (!string.IsNullOrEmpty(Area))
					DrawList.Areas.Add(new Area(Area, x, y, Height, Width));

				base.Draw(DrawList, x, y);
			}
		}

		class XmlCanvas : XmlRectangle
		{
			public string ColorDepth { get; set; }	// schema restricts values

			public string Type { get; set; }
			public int StrideMultiple { get; set; } = XmlScreen.StrideMultiple;
			public int WidthMultiple { get; set; } = XmlScreen.WidthMultiple;
			public int ItemWidth { get; set; }
			public override int Width
			{
				// Width is a field, stride is in the Visual. Round them up
				// as requested.
				get => m_Width;
				set
				{
					m_Width = (value + WidthMultiple - 1) & ~(WidthMultiple - 1); 
					Visual.Width = (m_Width + StrideMultiple - 1) & ~(StrideMultiple - 1); 
				}
			}

			public override void ElementComplete()
			{
				if (Width != 0 && ItemWidth != 0)
					throw new Exception("Only one of 'Width' or 'ItemWidth' attributes can be set.");
				if (Width == 0 && ItemWidth == 0)
					throw new Exception("One of 'Width' or 'ItemWidth' attributes must be set.");
				Width = m_Width;	// set it again in case WidthMultiple changed
				base.ElementComplete();
			}

			public override void Draw(DrawResults DrawList, int x, int y)
			{
				int iLeftCur;

				// Verify we have valid contents
				// Update position of each item
				iLeftCur = 0;
				foreach (Element el in Owner.Children)
				{
					if (el.Graphic as XmlItem != null)
					{
						el.Left += iLeftCur;
						iLeftCur += ItemWidth;
					}
					else if (ItemWidth != 0)
						throw new Exception("Canvas can only contain 'Item' elements when 'ItemWidth' attribute is set.");
				}

				if (iLeftCur != 0)
					Width = iLeftCur;
				else if (ItemWidth != 0)
					throw new Exception("Item element required when 'ItemWidth' attribute is set.");

				base.Draw(DrawList, x, y);
			}
		}

		class XmlItem : XmlGraphic
		{
			public override void ElementComplete()
			{
				if (((XmlCanvas)Owner.Parent.Graphic).ItemWidth == 0)
					throw new Exception("'ItemWidth' attribute must be set on Canvas to use 'Item' element.");
				base.ElementComplete();
			}
		}

		class XmlRectangle : XmlArea
		{
			public XmlRectangle()
			{
				Rectangle shape;

				shape = new Rectangle();
				shape.StrokeThickness = 0;
				Visual = shape;
			}
		}

		class XmlEllipse : XmlArea
		{
			public XmlEllipse()
			{
				Ellipse shape;

				shape = new Ellipse();
				shape.StrokeThickness = 0;
				Visual = shape;
			}

			public override void Draw(DrawResults DrawList, int x, int y)
			{
				if (!string.IsNullOrEmpty(Location))
				{
					// Use center as location. Error if not even pixel.
					if ((Width & 1) != 0 || (Height & 1) != 0)
						throw new Exception("Ellipse height and width must be even so center falls on an even pixel");
					DrawList.Locations.Add(new Location(Location, x + Width / 2, y + Height / 2));
					Location = null;	// Don't report in base class
				}

				base.Draw(DrawList, x, y);
			}

		}

		class XmlLine : XmlGraphic
		{
			public XmlLine()
			{
				Line shape;

				shape = new Line();
				Visual = shape;
			}

			public override void Draw(DrawResults DrawList, int x, int y)
			{
				Line line;

				line = (Line)Visual;
				if (Double.IsNaN(line.Height))
				{
					if (Owner.Parent.Graphic != null && Owner.Parent.Graphic.Height != 0)
						line.Height = Owner.Parent.Graphic.Height - Owner.Top;
					else
						line.Height = Math.Max(line.Y2, line.Y1) + line.StrokeThickness;
				}
				if (Double.IsNaN(line.Width))
				{
					if (Owner.Parent.Graphic != null && Owner.Parent.Graphic.Width != 0)
						line.Width = Owner.Parent.Graphic.Width - Owner.Left;
					else
						line.Width = Math.Max(line.X2, line.X1) + line.StrokeThickness;
				}

				base.Draw(DrawList, x, y);
			}
		}

		class XmlTextBlock : XmlArea
		{
			public XmlTextBlock()
			{
				Visual = new TextBlock();
				Visual.HorizontalAlignment = HorizontalAlignment.Center;
				Visual.VerticalAlignment = VerticalAlignment.Center;
			}

			public override Element Owner 
			{ 
				get => base.Owner; 
				set
				{
					// We want to know if Top/Left were set
					value.Top = IntNoValue;
					value.Left = IntNoValue;
					base.Owner = value;
				}
			}

			public override void ElementComplete()
			{
				if (!string.IsNullOrEmpty(Owner.Name))
					ExprContext.Variables[Owner.Name] = new TextSize((TextBlock)Visual);

				base.ElementComplete();
			}

			public override XmlGraphic Clone()
			{
				XmlGraphic graphic;

				graphic = base.Clone();
				if (!string.IsNullOrEmpty(graphic.Owner.Name))
					ExprContext.Variables[graphic.Owner.Name] = new TextSize((TextBlock)graphic.Visual);
				return graphic;
			}

			public override int Height { get; set; }
			public override int Width { get; set; }
			public string Font
			{
				set
				{
					XmlFont font;

					if (!XmlScreen.Fonts.TryGetValue(value, out font))
						throw new Exception($"No font named '{value}' was found.");

					if (font.FontFamily != null)
						FontFamily = font.FontFamily;

					if (font.FontWeight != null)
						FontWeight = font.FontWeight;

					if (font.FontStyle != null)
						FontStyle = font.FontStyle;

					if (font.FontStretch != null)
						FontStretch = font.FontStretch;

					if (font.FontSize != 0)
						SetAttribute("FontSize", font.FontSize.ToString());
				}
			}

			public override void Draw(DrawResults DrawList, int x, int y)
			{
				if (Owner.Parent.Graphic != null)
				{
					if (Width == 0 && Owner.Left == IntNoValue)
						Width = Owner.Parent.Graphic.Width;
					if (Height == 0 && Owner.Top == IntNoValue)
						Height = Owner.Parent.Graphic.Height;
				}
				if (Width == 0 || Height == 0)
				{
					Visual.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
					if (Width == 0)
						Width = (int)Math.Ceiling(Visual.DesiredSize.Width);
					if (Height == 0)
						Height = (int)Math.Ceiling(Visual.DesiredSize.Height);
				}
				base.Draw(DrawList, x, y);
			}

			public string FontFamily
			{
				set { ((TextBlock)Visual).FontFamily = new FontFamily(value); }
			}

			public string FontWeight
			{
				set
				{
					PropertyInfo prop;

					// Look it up by name
					prop = typeof(FontWeights).GetProperty(value);
					if (prop == null)
						throw new Exception("Invalid FontWeight");
					((TextBlock)Visual).FontWeight = (FontWeight)prop.GetValue(null);
				}
			}

			public string FontStyle
			{
				set
				{
					PropertyInfo prop;

					// Look it up by name
					prop = typeof(FontStyles).GetProperty(value);
					if (prop == null)
						throw new Exception("Invalid FontStyle");
					((TextBlock)Visual).FontStyle = (FontStyle)prop.GetValue(null);
				}
			}

			public string FontStretch
			{
				set
				{
					PropertyInfo prop;

					// Look it up by name
					prop = typeof(FontStretches).GetProperty(value);
					if (prop == null)
						throw new Exception("Invalid FontStretch");
					((TextBlock)Visual).FontStretch = (FontStretch)prop.GetValue(null);
				}
			}

			public override string Content
			{
				set { ((TextBlock)Visual).Text = value; }
			}
		}

		class XmlImage : XmlArea
		{
			public XmlImage()
			{
				Image image;

				image = new Image();
				Visual = image;
			}

			public string Source
			{
				set
				{
					BitmapImage bmp;
					Image image;
					string fileName;

					if (string.IsNullOrEmpty(value))
						return;

					fileName = System.IO.Path.Combine(SourceFolder, value);
					fileName = System.IO.Path.GetFullPath(fileName);

					bmp = new BitmapImage();
					bmp.BeginInit();
					bmp.UriSource = new Uri(fileName, UriKind.Absolute);
					bmp.EndInit();
					image = ((Image)Visual);
					image.Source = bmp;
				}
			}

			public override void Draw(DrawResults DrawList, int x, int y)
			{
				Image image;

				image = ((Image)Visual);
				if (image.Source == null && (Double.IsNaN(image.Height) || Double.IsNaN(image.Width)))
					throw new Exception("Image source or height and width must be set.");

				if (Double.IsNaN(image.Height))
					image.Height = ((BitmapSource)image.Source).PixelHeight;
				if (Double.IsNaN(image.Width))
					image.Width = ((BitmapSource)image.Source).PixelWidth;

				base.Draw(DrawList, x, y);
			}
		}

		class XmlRef : XmlGraphic
		{
			public string RefName
			{
				set
				{
					Element el;

					if (XmlScreen.Components.TryGetValue(value, out el))
						el.CloneTo(Owner);
					else
						throw new Exception($"No component named '{value}' was found.");
				}
			}
		}

		class XmlSet : XmlGraphic
		{
			const string RegExpressionInt = "\\s*((\"(?<val>([^\"\\\\]|\\\\.)*)\")|(?<val>([^;\"\\\\]|\\\\.)+))";
			const string RegExpressionStr = "\\s*(?<val>((\"(([^\"\\\\]|\\\\.)*)\")|([^;\"\\\\]|\\\\.)+)+)";
			const string RegLeftSide = "(((?<ref>[a-zA-Z0-9_$]+)\\.)|(?<ref>.{0}))(?<attr>[a-zA-Z0-9_$]+)\\s*=";
			const string RegAssignInt = RegLeftSide + RegExpressionInt;
			const string RegAssignStr = RegLeftSide + RegExpressionStr;

			static Regex s_regExpressionInt = new Regex("^" + RegExpressionInt + "\\s*$");
			static Regex s_regExpressionStr = new Regex("^" + RegExpressionStr + "\\s*$");
			static Regex s_regAssignInt = new Regex(@"^\s*" + RegAssignInt + @"(\s*;\s*" + RegAssignInt + @")*\s*;?\s*$");
			static Regex s_regAssignStr = new Regex(@"^\s*" + RegAssignStr + @"(\s*;\s*" + RegAssignStr + @")*\s*;?\s*$");

			protected static Regex s_regUnescape = new Regex(@"\\(.)");

			public string ShowValue { get; set; }

			public override string Content
			{
				set => Eval(value, false);
			}

			protected void Eval(string val, bool fIsString = false)
			{
				Match match;
				string refName;
				string attrName;

				if (ShowValue != null)
				{
					match = fIsString ? s_regExpressionStr.Match(val) : s_regExpressionInt.Match(val);
					if (match.Success && match.Groups["val"].Captures.Count == 1)
					{
						val = s_regUnescape.Replace(match.Groups["val"].Captures[0].Value, "$1");
						Values.Add(new ShowValue(ShowValue, fIsString ? (object)Element.EvalString(val) : (object)Element.EvalInt(val)));
						return;
					}
					throw new Exception($"Invalid Set expression: '{val}'");
				}

				match = fIsString ? s_regAssignStr.Match(val) : s_regAssignInt.Match(val);
				if (!match.Success)
					throw new Exception($"Invalid Set expression: '{val}'");

				for (int i = 0; i < match.Groups["attr"].Captures.Count; i++)
				{
					refName = match.Groups["ref"].Captures[i].Value;
					attrName = match.Groups["attr"].Captures[i].Value;
					val = s_regUnescape.Replace(match.Groups["val"].Captures[i].Value, "$1");

					if (string.IsNullOrEmpty(refName))
					{
						// Assigning a variable
						try
						{
							ExprContext.Variables[attrName] = fIsString ? (object)Element.EvalString(val) : (object)Element.EvalInt(val);
						}
						catch (Exception exc)
						{
							throw new Exception($"Error assigning to variable '{attrName}':\n{exc.Message}");
						}
					}
					else
					{
						// Assigning a property. Assign to all occurrences.
						bool fFound = false;
						if (Owner.Parent != null)
						{
							foreach (Element el in Owner.Parent.AllChildren())
							{
								if (el.Name == refName)
								{
									el.SetAttribute(attrName, fIsString ? Element.EvalString(val) : val);
									fFound = true;
								}
							}
						}
						if (!fFound)
							throw new Exception($"Set can't find an element named '{refName}'");
					}
				}
			}
		}

		class XmlSetString : XmlSet
		{
			public override string Content
			{
				set => Eval(value, true);
			}
		}

		class XmlGrid : XmlGraphic
		{
			public int RowHeight { get; set; }
			public int ColumnWidth { get; set; }
			public override void Draw(DrawResults DrawList, int x, int y)
			{
				int iRowCur;

				// Update positions of the columns
				iRowCur = 0;
				foreach (Element el in Owner.Children)
				{
					if (el.Graphic as XmlRow != null)
					{
						el.Top += iRowCur;
						iRowCur = el.Top + RowHeight;
					}
					else if (el.Graphic is XmlDefault)
						el.Children.Clear();
				}
			}
		}

		class XmlRow : XmlGraphic
		{
			public override void Draw(DrawResults DrawList, int x, int y)
			{
				XmlGrid grid;
				int iColCur;
				int iColWidth;

				// Update positions of the columns
				grid = Owner.Parent.Graphic as XmlGrid;
				if (grid == null)
					throw new Exception("Row must be within Grid");
				iColWidth = grid.ColumnWidth;

				iColCur = 0;
				foreach (Element el in Owner.Children)
				{
					if ((el.Graphic as XmlDefault)?.IsCopy == false)
					{
						// Ignore original Row default
						el.Children.Clear();
						continue;
					}
					el.Left += iColCur;
					iColCur = iColWidth + el.Left;
				}
			}
		}

		class XmlColumn : XmlGraphic
		{
			public override Element Owner
			{
				get { return base.Owner; }
				set
				{
					Element content;

					// Row may have default content
					content = value.Parent;
					if (content.Children.Count == 0 || (content.Children[0].Graphic as XmlDefault)?.IsCopy != false)
						content = value.Parent.Parent;  // Move up to Grid

					if (content.Children.Count != 0)
					{
						content = content.Children[0];
						if (content.Graphic.GetType() == typeof(XmlDefault))
						{
							// Default contents exists, substitute it
							content.CloneTo(value);
							// Mark the copy to distinguish it from user's Default
							((XmlDefault)value.Graphic).IsCopy = true;
							return;
						}
					}

					base.Owner = value;
				}
			}
		}

		class XmlColumnNoDefault : XmlGraphic
		{
		}

		class XmlDefault : XmlGraphic
		{
			public bool IsCopy { get; set; }
		}

		class XmlFont : XmlGraphic
		{
			public string FontFamily { get; set; }
			public string FontWeight { get; set; }
			public string FontStyle { get; set; }
			public string FontStretch { get; set; }
			public int FontSize { get; set; }

			public string Font
			{
				set
				{
					XmlFont font;

					if (!XmlScreen.Fonts.TryGetValue(value, out font))
						throw new Exception($"No font named '{value}' was found.");

					if (font.FontFamily != null)
						FontFamily = font.FontFamily;

					if (font.FontWeight != null)
						FontWeight = font.FontWeight;

					if (font.FontStyle != null)
						FontStyle = font.FontStyle;

					if (font.FontStretch != null)
						FontStretch = font.FontStretch;

					if (font.FontSize != 0)
						FontSize = font.FontSize;
				}
			}

			public override void ElementComplete()
			{
				Fonts[Owner.Name] = this;   // add new or replace existing
				base.ElementComplete();
			}
		}

		class XmlColor : XmlGraphic
		{
			int m_color;

			public string ColorDepth { get; set; }  // schema restricts values

			public string Color
			{
				set
				{
					Color color;

					color = ColorFromString(value);
					m_color = (color.R << 16) | (color.G << 8) | color.B;
				}
			}

			public override void ElementComplete()
			{
				int r, g, b;

				// Flatten color to match depth, if specified
				if (!string.IsNullOrEmpty(ColorDepth))
				{
					r = m_color >> 16;
					g = (m_color >> 8) & 0xFF;
					b = m_color & 0xFF;

					switch (ColorDepth)
					{
						case StrColorDepth8:
							r = LimitColor(r, BpC.R8);
							g = LimitColor(g, BpC.G8);
							b = LimitColor(b, BpC.B8);
							break;

						case StrColorDepth16:
							r = LimitColor(r, BpC.R16);
							g = LimitColor(g, BpC.G16);
							b = LimitColor(b, BpC.B16);
							break;
					}
					m_color = (r << 16) | (g << 8) | b;
				}
				Colors[Owner.Name] = m_color;   // will add new or replace existing
				base.ElementComplete();
			}
		}

		class Element
		{
			public Element(string type, Element parent)
			{
				Type typGraphic;

				Parent = parent;
				Children = new List<Element>();
				if (ElementTypes.TryGetValue(type, out typGraphic))
				{
					Graphic = (XmlGraphic)Activator.CreateInstance(typGraphic);
					Graphic.Owner = this;
				}
			}

			#region Public Properties

			public string Name { get; set; }
			public int Top { get; set; }
			public int Left { get; set; }
			public int Width { get { return Graphic.Width; } }
			public int Height { get { return Graphic.Height; } }
			public List<Element> Children { get; protected set; }
			public XmlGraphic Graphic { get; set; }
			public Element Parent { get; set; }
			public XElement Node { get; set; }
			public string Content
			{
				set
				{
					if (Graphic != null)
						Graphic.Content = value;
				}
			}

			#endregion

			static readonly Dictionary<string, Type> ElementTypes = new Dictionary<string, Type>
			{
				{ "Rectangle",  typeof(XmlRectangle) },
				{ "Ellipse",    typeof(XmlEllipse) },
				{ "Line",       typeof(XmlLine) },
				{ "TextBlock",  typeof(XmlTextBlock) },
				{ "Image",      typeof(XmlImage) },
				{ "Canvas",     typeof(XmlCanvas) },
				{ "Ref",        typeof(XmlRef) },
				{ "Set",        typeof(XmlSet) },
				{ "SetString",  typeof(XmlSetString) },
				{ "Grid",       typeof(XmlGrid) },
				{ "Row",        typeof(XmlRow) },
				{ "Column",     typeof(XmlColumn) },
				{ "ColumnNoDefault", typeof(XmlColumnNoDefault) },
				{ "Default",    typeof(XmlDefault) },
				{ "Font",       typeof(XmlFont) },
				{ "Color",      typeof(XmlColor) },
				{ "Item",		typeof(XmlItem) },
			};

			public void ElementComplete()
			{
				Graphic?.ElementComplete();
			}

			public Element CloneTo(Element el)
			{
				el.Name = Name;
				el.Node = Node;
				el.Graphic = Graphic;
				if (Graphic != null)
				{
					el.Graphic = Graphic.Clone();
					el.Graphic.Owner = el;
				}
				el.Top = Top;
				el.Left = Left;
				el.Children.Clear();
				foreach (Element elChild in Children)
					el.Children.Add(elChild.Clone(el));

				return el;
			}

			public Element Clone(Element parent)
			{
				return CloneTo(new Element("", parent));
			}

			public void SetAttribute(string attr, string value)
			{
				switch (attr)
				{
					case "Name":
						Name = value;
						break;

					case "Top":
						Top = EvalInt(value);
						break;

					case "Left":
						Left = EvalInt(value);
						break;

					default:
						Graphic?.SetAttribute(attr, value);
						break;
				}
			}

			public IEnumerable<Element> AllChildren()
			{
				foreach (Element el in Children)
				{
					yield return el;

					foreach (Element elChild in el.AllChildren())
						yield return elChild;
				}
			}

			public void Draw(DrawResults DrawList, int x, int y)
			{
				// IntNoValue is used only by TextBlock to know if 
				// Left or Top has been set.
				x += Left == IntNoValue ? 0 : Left;
				y += Top == IntNoValue ? 0 : Top;
				try
				{
					if (Graphic != null)
						Graphic.Draw(DrawList, x, y);

					foreach (Element el in Children)
						el.Draw(DrawList, x, y);
				}
				catch (CaughtException)
				{
					throw;  // already reported
				}
				catch (Exception exc)
				{
					var info = (IXmlLineInfo)Node;
					string msg = exc.InnerException?.Message ?? exc.Message;
					throw new CaughtException($"Error at line {info.LineNumber}:\n{msg}");
				}
			}

			static public int EvalInt(string expr)
			{
				int i;

				if (int.TryParse(expr, out i))
					return i;
				try
				{
					i = ExprContext.CompileGeneric<int>(expr).Evaluate();
				}
				catch
				{
					throw new Exception($"Error attempting to evaluate expression: '{expr}'");
				}
				return i;
			}

			static public string EvalString(string expr)
			{
				string str;

				try
				{
					str = ExprContext.CompileGeneric<string>(expr).Evaluate();
				}
				catch
				{
					throw new Exception($"Error attempting to evaluate expression: '{expr}'");
				}
				return str;
			}
		}

		class DrawResults
		{
			public DrawResults()
			{
				Visual = new ContainerVisual();
				Locations = new List<Location>();
				Areas = new List<Area>();
				HotSpots = new List<HotSpot>();
			}
			public ContainerVisual Visual { get; protected set; }
			public List<Location> Locations { get; protected set; }
			public List<Area> Areas { get; protected set; }
			public List<HotSpot> HotSpots { get; protected set; }
		}

		#endregion


		#region Constructor

		public XmlScreen()
		{
			Components = new Dictionary<string, Element>();
			Images = new List<NamedBitmap>();
			ExprContext = new ExpressionContext();
			Fonts = new Dictionary<string, XmlFont>();
			Colors = new Dictionary<string, int>();
			Values = new List<ShowValue>();
			StrideMultiple = 1;
			WidthMultiple = 1;
		}

		#endregion


		#region Fields

		static Dictionary<string, Element> Components;
		static Dictionary<string, XmlFont> Fonts;
		static Dictionary<string, int> Colors;
		static ExpressionContext ExprContext;
		static List<ShowValue> Values;
		static int StrideMultiple;
		static int WidthMultiple;
		static string SourceFolder;
		List<NamedBitmap> Images;

		#endregion


		#region Public Methods

		internal List<NamedBitmap> ParseXml(XDocument xml, string strFolder)
		{
			XElement node = (XElement)xml.Root;
			SourceFolder = strFolder;

			// Look for attributes on root node
			if (node.HasAttributes)
			{
				foreach (XAttribute attr in node.Attributes())
				{
					switch (attr.Name.LocalName)
					{
						case "StrideMultiple":
							StrideMultiple = int.Parse(attr.Value);
							break;

						case "WidthMultiple":
							WidthMultiple = int.Parse(attr.Value);
							break;

						// Additional top-level attributes go here
					}
				}
			}

			foreach (XElement el in node.Elements())
			{
				switch (el.Name.LocalName)
				{
					case "Component":
						DefineComponent(el);
						break;

					case "Canvas":
						DefineCanvas(el);
						break;

					default:
						// Element handles itself
						BuildElement(el);
						break;
				}
			}
			return Images;
		}

		internal List<KeyValuePair<string, int>> GetColors()
		{
			return Colors.ToList();
		}

		internal List<ShowValue> GetValues()
		{
			return Values;
		}

		#endregion


		#region Private Methods

		void DefineComponent(XElement node)
		{
			Element el;

			el = BuildElement(node);
			try
			{
				Components.Add(el.Name, el);
			}
			catch (Exception exc)
			{
				var info = (IXmlLineInfo)node;
				string msg = exc.InnerException?.Message ?? exc.Message;
				throw new CaughtException($"Error at line {info.LineNumber} while adding Component '{el.Name}':\n{msg}");
			}
		}

		void DefineCanvas(XElement node)
		{
			Element el;
			DrawResults DrawList;
			NamedBitmap bmp;
			XmlCanvas canvas;

			el = BuildElement(node);
			DrawList = new DrawResults();
			el.Draw(DrawList, 0, 0);
			canvas = (XmlCanvas)el.Graphic;
			bmp = new NamedBitmap(el.Name, el.Width, el.Height, (int)canvas.Visual.Width, canvas.ColorDepth, canvas.Type);
			bmp.Locations = DrawList.Locations;
			bmp.Areas = DrawList.Areas;
			bmp.HotSpots = DrawList.HotSpots;
			((RenderTargetBitmap)bmp.Bitmap).Render(DrawList.Visual);
			Images.Add(bmp);
		}

		Element BuildElement(XElement node, Element parent = null)
		{
			Element el;
			XAttribute attrError;

			attrError = null;
			try
			{
				el = new Element(node.Name.LocalName, parent);
				el.Node = node;		// for tracking error location
				if (node.HasAttributes)
				{
					foreach (XAttribute attr in node.Attributes())
					{
						attrError = attr;
						el.SetAttribute(attr.Name.LocalName, attr.Value);
					}
				}
				attrError = null;
				el.ElementComplete();

				if (!string.IsNullOrEmpty(node.Value))
					el.Content = node.Value;

				foreach (XElement nodeChild in node.Elements())
					el.Children.Add(BuildElement(nodeChild, el));
			}
			catch (CaughtException)
			{
				throw;  // already reported
			}
			catch (Exception exc)
			{
				var info = (IXmlLineInfo)node;
				string strErr = attrError != null ? $" setting attribute '{attrError.Name.LocalName}' to '{attrError.Value}'" : "";
				string msg = exc.InnerException?.Message ?? exc.Message;
				throw new CaughtException($"Error at line {info.LineNumber}{strErr}:\n{msg}");
			}
			return el;
		}

		#endregion
	}


	#region Result Types

	class CaughtException : Exception
	{ 
		public CaughtException(string message) : base(message)
		{ }
	}

	class Location
	{
		public Location(string name, int x, int y)
		{
			Name = name;
			X = x;
			Y = y;
		}
		public string Name { get; protected set; }
		public int X { get; protected set; }
		public int Y { get; protected set; }
	}

	class Area : Location
	{
		public Area(string name, int x, int y, int height, int width) :
			base(name, x, y)
		{
			Height = height;
			Width = width;
		}
		public int Width { get; protected set; }
		public int Height { get; protected set; }
	}

	class HotSpot
	{
		public HotSpot(string name, string group, int x, int y, int width, int height)
		{
			Name = name;
			Group = group;
			MinX = x;
			MaxX = x + width - 1;
			MinY = y;
			MaxY = y + height - 1;
		}
		public string Name { get; set; }
		public string Group { get; set; }
		public int MinX { get; set; }
		public int MaxX { get; set; }
		public int MinY { get; set; }
		public int MaxY { get; set; }
	}

	class ShowValue
	{
		public ShowValue(string name, object value)
		{
			Name = name;
			Value = value;
		}
		public string Name { get; set; }
		public object Value { get; set; }
	}

	class NamedBitmap
	{
		public NamedBitmap(string name, int width, int height, int stride, string depth, string type)
		{
			Name = name;
			Height = height;
			Width = width;
			Stride = stride;
			ColorDepth = depth;
			Type = type;
			Bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);

			switch (depth)
			{
				case XmlScreen.StrColorDepth8:
					BytesPerPixel = 1;
					break;

				case XmlScreen.StrColorDepth16:
					BytesPerPixel = 2;
					break;

				case XmlScreen.StrColorDepth24:
					BytesPerPixel = 3;
					break;

				default:
					throw new Exception($"ColorDepth property of canvas '{Name}' has invalid value '{ColorDepth}'");
			}
		}
		public string Name { get; protected set; }
		public int Height { get; protected set; }
		public int Width { get; protected set; }
		public int Stride { get; protected set; }
		public string ColorDepth { get; protected set; }
		public string Type { get; protected set; }
		public int BytesPerPixel { get; protected set; }
		public BitmapSource Bitmap { get; set; }
		public List<Location> Locations { get; set; }
		public List<Area> Areas { get; set; }
		public List<HotSpot> HotSpots { get; set; }
	}

	#endregion
}
