using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

namespace ScreenDesigner
{
	class XmlImage
	{
		#region Types

		class XmlGraphic
		{
			string m_xmlClone;

			public virtual int Height 
			{ 
				get { return (int)Graphic.Height; }
				set { Graphic.Height = value; }
			}

			public virtual int Width
			{ 
				get { return (int)Graphic.Width; }
				set { Graphic.Width = value; }
			}

			public virtual string Value
			{
				set { }
			}

			public FrameworkElement Graphic { get; set; }
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
					prop = Graphic?.GetType().GetProperty(Attr);
					obj = Graphic;
				}

				if (prop != null)
				{
					if (prop.PropertyType == typeof(string))
						prop.SetValue(obj, Value);
					else if (prop.PropertyType == typeof(Brush))
						prop.SetValue(obj, new SolidColorBrush(ColorFromString(Value)));
					else
						prop.SetValue(obj, Convert.ChangeType(int.Parse(Value), prop.PropertyType));
				}
			}

			public virtual void Draw(DrawResults DrawList, int x, int y)
			{
				if (Graphic != null)
				{
					Graphic.Arrange(new Rect(x, y, Width, Height));
					DrawList.Visual.Children.Add(Graphic);
				}
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

			public XmlGraphic Clone()
			{
				XmlGraphic graphic;

				graphic = (XmlGraphic)MemberwiseClone();
				if (Graphic != null)
				{
					if (m_xmlClone == null)
						m_xmlClone = XamlWriter.Save(Graphic);
					graphic.Graphic = (FrameworkElement)XamlReader.Load(new XmlTextReader(new StringReader(m_xmlClone)));
				}
				return graphic;
			}
		}

		class XmlRectangle : XmlGraphic
		{
			public XmlRectangle()
			{
				Rectangle rect;

				rect = new Rectangle();
				rect.StrokeThickness = 0;
				Graphic = rect;
			}
		}

		class XmlTextBlock : XmlGraphic
		{
			public XmlTextBlock()
			{
				Graphic = new TextBlock();
				Graphic.HorizontalAlignment = HorizontalAlignment.Center;
				Graphic.VerticalAlignment = VerticalAlignment.Center;
			}
			public override int Height { get; set; }
			public override int Width { get; set; }

			public override Element Owner 
			{
				get { return base.Owner; }
				set
				{
					base.Owner = value;
					Width = Owner.Parent.Graphic.Width;
					Height = Owner.Parent.Graphic.Height;
				}
			}

			public string FontFamily
			{
				set { ((TextBlock)Graphic).FontFamily = new FontFamily(value); }
			}
		}

		class XmlHotSpot : XmlGraphic
		{
			public override int Height { get; set; }
			public override int Width { get; set; }
			public override void Draw(DrawResults DrawList, int x, int y)
			{
				HotSpot spot;

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
					XmlImage.Components[value].CloneTo(Owner);
				}
			}
		}

		class XmlSet : XmlGraphic
		{
			static Regex s_regAssign = new Regex(@"([a-zA-Z0-9_$]+).([a-zA-Z0-9_$]+)\s*=\s*(.+)$");
			static Regex s_regAssignRef = new Regex(@"([a-zA-Z0-9_$]+)\s*=\s*(.+)$");

			public string RefName { get; set; }

			public override string Value
			{
				set
				{
					Match match;
					Element el;
					string attr;
					string val;
					string refName;
					string[] arExpr;
					string exprTrim;

					arExpr = value.Split(';');
					foreach (string expr in arExpr)
					{
						exprTrim = expr.Trim();
						if (exprTrim.Length == 0)
							continue;

						if (RefName == null)
						{
							match = s_regAssign.Match(exprTrim);
							if (!match.Success)
								throw new Exception("Invalid Set"); // UNDONE: improve error reporting
							refName = match.Groups[1].Value;
							attr = match.Groups[2].Value;
							val = match.Groups[3].Value;
						}
						else
						{
							match = s_regAssignRef.Match(exprTrim);
							if (!match.Success)
								throw new Exception("Invalid Set"); // UNDONE: improve error reporting
							refName = RefName;
							attr = match.Groups[1].Value;
							val = match.Groups[2].Value;
						}
						el = Owner.Parent.FindChild(refName);
						if (el != null)
							el.SetAttribute(attr, val);
					}
				}
			}
		}

		class XmlData : XmlGraphic
		{
			public override string Value
			{
				set
				{
					Owner.Parent.Value = value;
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
						iRowCur += ColumnWidth;
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
					iColCur += iColWidth;
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
			public int Top { get; set; }
			public int Left { get; set; }
			public int Width { get { return (int)Graphic.Width; } }
			public int Height { get { return (int)Graphic.Height; } }
			public List<Element> Children { get; protected set; }
			public XmlGraphic Graphic { get; set; }
			public Element Parent { get; set; }
			public string Group { get; set; }
			public string Location { get; set; }
			public string Value
			{
				set 
				{
					if (Graphic != null)
						Graphic.Value = value;
				}
			}

			#endregion

			static readonly Dictionary<string, Type> ElementTypes = new Dictionary<string, Type>
			{
				{ "Rectangle",	typeof(XmlRectangle) },
				{ "TextBlock",	typeof(XmlTextBlock) },
				{ "HotSpot",	typeof(XmlHotSpot) },
				{ "Image",		typeof(XmlRectangle) },
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

			public void SetAttribute(string Attr, string Value)
			{
				if (Attr == "Name")
				{
					Name = Value;
					return;		// don't set it in Graphics
				}

				Graphic?.SetAttribute(Attr, Value);

				// Assign our own properties
				switch (Attr)
				{
					case "Top":
						Top = int.Parse(Value);
						break;

					case "Left":
						Left = int.Parse(Value);
						break;

					case "Group":
						Group = Value;
						break;

					case "Location":
						Location = Value;
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

		public XmlImage()
		{
			Components = new Dictionary<string, Element>();
			Images = new List<NamedBitmap>();
		}

		#endregion


		#region Fields

		static Dictionary<string, Element> Components;
		List<NamedBitmap> Images;

		#endregion


		#region Public Methods

		internal List<NamedBitmap> ParseXml(XmlDocument xml)
		{
			XmlNode elProject;

			elProject = xml.DocumentElement;
			foreach (XmlNode el in elProject.ChildNodes)
			{
				switch (el.Name)
				{
					case "Component":
						DefineComponent(el);
						break;

					case "Image":
						DefineImage(el);
						break;
				}
			}
			return Images;
		}

		#endregion


		#region Private Methods

		void DefineComponent(XmlNode node)
		{
			Element el;

			el = BuildElement(node);
			Components.Add(el.Name, el);
		}

		void DefineImage(XmlNode node)
		{
			Element el;
			DrawResults DrawList;
			NamedBitmap bmp;

			el = BuildElement(node);
			DrawList = new DrawResults();
			el.Draw(DrawList, 0, 0);
			bmp = new NamedBitmap(el.Name, el.Width, el.Height);
			bmp.Locations = DrawList.Locations;
			bmp.HotSpots = DrawList.HotSpots;
			bmp.Bitmap.Render(DrawList.Visual);
			Images.Add(bmp);
		}

		Element BuildElement(XmlNode node, Element parent = null)
		{
			Element el;

			el = new Element(node.Name, parent);
			if (node.Attributes != null)
			{
				foreach (XmlAttribute attr in node.Attributes)
					el.SetAttribute(attr.Name, attr.Value);
			}

			if (node.Value != null)
				el.Value = node.Value;

			foreach (XmlNode nodeChild in node.ChildNodes)
				el.Children.Add(BuildElement(nodeChild, el));

			return el;
		}

		#endregion
	}


	#region Result Types

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
		public NamedBitmap(string name, int width, int height)
		{
			Name = name;
			Height = height;
			Width = width;
			Bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
		}
		public string Name { get; protected set; }
		public int Height { get; protected set; }
		public int Width { get; protected set; }
		public RenderTargetBitmap Bitmap { get; protected set; }
		public List<Location> Locations { get; set; }
		public List<HotSpot> HotSpots { get; set; }
	}

	#endregion
}
