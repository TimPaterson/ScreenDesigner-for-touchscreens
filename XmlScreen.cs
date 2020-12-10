using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
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

namespace ScreenDesigner
{
	class XmlScreen
	{
		#region Types

		class XmlGraphic
		{
			string m_xmlClone;

			public virtual int Height 
			{ 
				get { return (int)Visual.Height; }
				set { Visual.Height = value; }
			}

			public virtual int Width
			{ 
				get { return (int)Visual.Width; }
				set { Visual.Width = value; }
			}

			public virtual string Content
			{
				set { }
			}

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
					if (prop != null)
						m_xmlClone = null;	// invalide cached copy
				}

				if (prop != null)
				{
					if (prop.PropertyType == typeof(string))
						prop.SetValue(obj, Value);
					else if (prop.PropertyType == typeof(Brush))
						prop.SetValue(obj, new SolidColorBrush(ColorFromString(Value)));
					else
						prop.SetValue(obj, Convert.ChangeType(Element.EvalInt(Value), prop.PropertyType));
				}
			}

			public virtual void Draw(DrawResults DrawList, int x, int y)
			{
				if (Visual != null)
				{
					Visual.Arrange(new Rect(x, y, Width, Height));
					DrawList.Visual.Children.Add(Visual);
				}
			}

			public XmlGraphic Clone()
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

				try
				{
					val = (int)new Int32Converter().ConvertFromString(color);
					return Color.FromRgb((byte)(val >> 16), (byte)((val >> 8) & 0xff), (byte)(val & 0xff));
				}
				catch (Exception exc)
				{
					Debug.WriteLine("Expected exception: " + exc.Message);
				}

				// Try looking it up by name
				prop = typeof(Colors).GetProperty(color);
				if (prop == null)
					throw new Exception("Invalid color");
				return (Color)prop.GetValue(null);
			}
		}

		class XmlCanvas : XmlRectangle
		{
			public string Folder { get; set; }
		}

		class XmlRectangle : XmlGraphic
		{
			public XmlRectangle()
			{
				Rectangle shape;

				shape = new Rectangle();
				shape.StrokeThickness = 0;
				Visual = shape;
			}
		}

		class XmlEllipse : XmlGraphic
		{
			public XmlEllipse()
			{
				Ellipse shape;

				shape = new Ellipse();
				shape.StrokeThickness = 0;
				Visual = shape;
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
					line.Height = Math.Abs(line.Y2 - line.Y1) + line.StrokeThickness;
				if (Double.IsNaN(line.Width))
					line.Width = Math.Abs(line.X2 - line.X1) + line.StrokeThickness;

				base.Draw(DrawList, x, y);
			}
		}

		class XmlTextBlock : XmlGraphic
		{
			public XmlTextBlock()
			{
				Visual = new TextBlock();
				Visual.HorizontalAlignment = HorizontalAlignment.Center;
				Visual.VerticalAlignment = VerticalAlignment.Center;
			}

			public override int Height { get; set; }
			public override int Width { get; set; }

			public override void Draw(DrawResults DrawList, int x, int y)
			{
				if (Owner.Parent.Graphic != null)
				{
					if (Width == 0)
						Width = Owner.Parent.Graphic.Width;
					if (Height == 0)
						Height = Owner.Parent.Graphic.Height;
				}
				else if (Width == 0 || Height == 0)
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

		class XmlImage : XmlGraphic
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

					bmp = new BitmapImage();
					bmp.BeginInit();
					bmp.UriSource = new Uri(value, UriKind.RelativeOrAbsolute);
					bmp.EndInit();
					image = ((Image)Visual);
					image.Source = bmp;
					if (Double.IsNaN(image.Height))
						image.Height = bmp.PixelHeight;
					if (Double.IsNaN(image.Width))
						image.Width = bmp.PixelWidth;
				}
			}
		}

		class XmlHotSpot : XmlGraphic
		{
			public override int Height { get; set; }
			public override int Width { get; set; }
			public override void Draw(DrawResults DrawList, int x, int y)
			{
				HotSpot spot;

				if (Owner.Parent.Graphic != null)
				{
					if (Width == 0)
						Width = Owner.Parent.Graphic.Width;
					if (Height == 0)
						Height = Owner.Parent.Graphic.Height;
				}
				spot = new HotSpot();
				spot.Name = Owner.Name;
				spot.Group = Owner.Group;
				spot.MinX = x;
				spot.MaxX = x + Width - 1;
				spot.MinY = y;
				spot.MaxY = y + Height - 1;
				DrawList.HotSpots.Add(spot);
			}

		}

		class XmlRef : XmlGraphic
		{
			public string RefName
			{
				set
				{
					XmlScreen.Components[value].CloneTo(Owner);
				}
			}
		}

		class XmlSet : XmlGraphic
		{
			const string RegAssignRef = "(?<attr>[a-zA-Z0-9_$]+)\\s*=\\s*((\"(?<val>([^\"\\\\]|\\\\\")*)\")|(?<val>([^;\"\\\\]|\\\\.)+))";
			const string RegAssign = @"(?<ref>[a-zA-Z0-9_$]+)." + RegAssignRef;

			static Regex s_regAssignRef = new Regex(@"^\s*" + RegAssignRef + @"(\s*;\s*" + RegAssignRef + @")*\s*;?\s*$");
			static Regex s_regAssign = new Regex(@"^\s*" + RegAssign + @"(\s*;\s*" + RegAssign + @")*\s*;?\s*$");
			static Regex s_regUnescape = new Regex(@"\\(.)");

			public string RefName { get; set; }

			public override string Content
			{
				set
				{
					Match match;
					Element el;
					string refName;
					string val;

					refName = RefName;
					if (RefName == null)
						match = s_regAssign.Match(value);
					else
						match = s_regAssignRef.Match(value);

					if (!match.Success)
						throw new Exception($"Invalid Set expression: '{value}'");

					for (int i = 0; i < match.Groups["attr"].Captures.Count; i++)
					{
						if (RefName == null)
							refName = match.Groups["ref"].Captures[i].Value;
						el = Owner.Parent.FindChild(refName);
						if (el != null)
						{
							val = s_regUnescape.Replace(match.Groups["val"].Captures[i].Value, "$1");
							el.SetAttribute(match.Groups["attr"].Captures[i].Value, val);
						}

					}
				}
			}
		}

		class XmlData : XmlGraphic
		{
			public override string Content
			{
				set
				{
					Owner.Parent.Content = value;
				}
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
					else if (el.Graphic as XmlDefault != null)
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
					Element grid;
					Element content;

					// Get default content
					grid = value.Parent.Parent;
					content = grid.Children[0];
					if (content.Graphic.GetType() == typeof(XmlDefault))
					{
						// Default contents exists, substitute it
						content.CloneTo(value);
					}
					else
						base.Owner = value;
				}
			}
		}

		class XmlDefault : XmlGraphic
		{
		}

		class Element
		{
			public Element(string type, Element parent)
			{
				Type typGraphic;

				Parent = parent;
				Group = parent?.Group;
				Children = new List<Element>();
				if (ElementTypes.TryGetValue(type, out typGraphic))
				{
					Graphic = (XmlGraphic)Activator.CreateInstance(typGraphic);
					Graphic.Owner = this;
				}
			}

			#region Public Properties

			public string Name { get; set; }
			public object Value { get; set; }
			public int Top { get; set; }
			public int Left { get; set; }
			public int Width { get { return (int)Graphic.Width; } }
			public int Height { get { return (int)Graphic.Height; } }
			public List<Element> Children { get; protected set; }
			public XmlGraphic Graphic { get; set; }
			public Element Parent { get; set; }
			public string Group { get; set; }
			public string Location { get; set; }
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
				{ "Ellipse",	typeof(XmlEllipse) },
				{ "Line",		typeof(XmlLine) },
				{ "TextBlock",  typeof(XmlTextBlock) },
				{ "Image",		typeof(XmlImage) },
				{ "HotSpot",	typeof(XmlHotSpot) },
				{ "Canvas",		typeof(XmlCanvas) },
				{ "Ref",		typeof(XmlRef) },
				{ "Set",		typeof(XmlSet) },
				{ "#text",		typeof(XmlData) },
				{ "Grid",       typeof(XmlGrid) },
				{ "Row",		typeof(XmlRow) },
				{ "Column",		typeof(XmlColumn) },
				{ "Default",	typeof(XmlDefault) },
			};

			public Element CloneTo(Element el)
			{
				el.Name = Name;
				el.Top = Top;
				el.Left = Left;
				el.Group = Group;
				el.Location = Location;
				el.Graphic = Graphic;
				if (Graphic != null)
				{
					el.Graphic = Graphic.Clone();
					el.Graphic.Owner = el;
				}
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

					case "Group":
						Group = value;
						break;

					case "Location":
						Location = value;
						break;

					case "Value":
						if (Name == null)
							throw new Exception("Name property must be set before setting Value.");
						Value = EvalInt(value);
						ExprContext.Variables[Name] = Value;
						break;

					default:
						Graphic?.SetAttribute(attr, value);
						break;
				}
			}

			public Element FindChild(string name)
			{
				Element el;

				foreach (Element elChild in Children)
				{
					if (elChild.Name == name)
						return elChild;

					el = elChild.FindChild(name);
					if (el != null)
						return el;
				}
				return null;
			}

			public void Draw(DrawResults DrawList, int x, int y)
			{
				x += Left;
				y += Top;
				if (Location != null)
					DrawList.Locations.Add(new Location(Location, x, y));
				if (Graphic != null)
					Graphic.Draw(DrawList, x, y);
				foreach (Element el in Children)
					el.Draw(DrawList, x, y);
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
					throw new Exception("Error attempting to evaluate expression");
				}
				return i;
			}
		}

		class DrawResults
		{
			public DrawResults()
			{
				Visual = new ContainerVisual();
				Locations = new List<Location>();
				HotSpots = new List<HotSpot>();
			}
			public ContainerVisual Visual { get; protected set; }
			public List<Location> Locations { get; protected set; }
			public List<HotSpot> HotSpots { get; protected set; }
		}

		#endregion


		#region Constructor

		public XmlScreen()
		{
			Components = new Dictionary<string, Element>();
			Images = new List<NamedBitmap>();
			ExprContext = new ExpressionContext();
		}

		#endregion


		#region Fields

		static Dictionary<string, Element> Components;
		static ExpressionContext ExprContext;
		List<NamedBitmap> Images;

		#endregion


		#region Public Methods

		internal List<NamedBitmap> ParseXml(XDocument xml)
		{
			foreach (XElement el in ((XElement)(xml.FirstNode)).Elements())
			{
				switch (el.Name.LocalName)
				{
					case "Component":
						DefineComponent(el);
						break;

					case "Canvas":
						DefineCanvas(el);
						break;

					case "Set":
						// Don't need result element, 
						// value is put in ExprContext
						BuildElement(el);
						break;
				}
			}
			return Images;
		}

		#endregion


		#region Private Methods

		void DefineComponent(XElement node)
		{
			Element el;

			el = BuildElement(node);
			Components.Add(el.Name, el);
		}

		void DefineCanvas(XElement node)
		{
			Element el;
			DrawResults DrawList;
			NamedBitmap bmp;

			el = BuildElement(node);
			DrawList = new DrawResults();
			el.Draw(DrawList, 0, 0);
			bmp = new NamedBitmap(el.Name, el.Width, el.Height, ((XmlCanvas)el.Graphic).Folder);
			bmp.Locations = DrawList.Locations;
			bmp.HotSpots = DrawList.HotSpots;
			bmp.Bitmap.Render(DrawList.Visual);
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
				if (node.HasAttributes)
				{
					foreach (XAttribute attr in node.Attributes())
					{
						attrError = attr;
						el.SetAttribute(attr.Name.LocalName, attr.Value);
					}
				}
				attrError = null;

				if (!string.IsNullOrEmpty(node.Value))
					el.Content = node.Value;

				foreach (XElement nodeChild in node.Elements())
					el.Children.Add(BuildElement(nodeChild, el));
			}
			catch (CaughtException)
			{
				throw;	// already reported
			}
			catch (Exception exc)
			{
				var info = (IXmlLineInfo)node;
				string strErr = attrError != null ? $" setting attribute '{attrError.Name.LocalName}' to '{attrError.Value}'" : "";
				throw new CaughtException($"Error at line {info.LineNumber}{strErr}:\n{exc.Message}");
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

	class HotSpot
	{
		public string Name { get; set; }
		public string Group { get; set; }
		public int MinX { get; set; }
		public int MaxX { get; set; }
		public int MinY { get; set; }
		public int MaxY { get; set; }
	}

	class NamedBitmap
	{
		public NamedBitmap(string name, int width, int height, string folder)
		{
			Name = name;
			Height = height;
			Width = width;
			Folder = folder;
			Bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
		}
		public string Name { get; protected set; }
		public int Height { get; protected set; }
		public int Width { get; protected set; }
		public string Folder { get; protected set; }
		public RenderTargetBitmap Bitmap { get; protected set; }
		public List<Location> Locations { get; set; }
		public List<HotSpot> HotSpots { get; set; }
	}

	#endregion
}
