using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
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
					else
						prop.SetValue(obj, Convert.ChangeType(int.Parse(Value), prop.PropertyType));
				}
			}

			public void Draw(ContainerVisual DrawList, int x, int y)
			{
				if (Graphic != null)
				{
					Graphic.Arrange(new Rect(x, y, Width, Height));
					DrawList.Children.Add(Graphic);
				}
			}

			protected static Color ColorFromString(string color)
			{
				int val;

				val = (int)new Int32Converter().ConvertFromString(color);
				return Color.FromRgb((byte)(val >> 16), (byte)((val >> 8) & 0xff), (byte)(val & 0xff));
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

			public string Stroke
			{
				set { ((Rectangle)Graphic).Stroke = new SolidColorBrush(ColorFromString(value)); }
			}

			public string Fill
			{
				set { ((Rectangle)Graphic).Fill = new SolidColorBrush(ColorFromString(value)); }
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
		}

		class XmlHotSpot : XmlGraphic
		{
			public override int Height { get; set; }
			public override int Width { get; set; }

		}

		class Element
		{
			internal class ChildCollection : KeyedCollection<string, Element>
			{
				protected override string GetKeyForItem(Element item)
				{
					return item.Name;
				}
			}
			
			public Element(string type, Element parent)
			{
				Type typGraphic;

				Parent = parent;
				if (ElementTypes.TryGetValue(type, out typGraphic))
				{
					Graphic = (XmlGraphic)Activator.CreateInstance(typGraphic);
					Graphic.Owner = this;
				}
				Children = new ChildCollection();
			}

			#region Public Properties

			public string Name { get; set; }
			public int Top { get; set; }
			public int Left { get; set; }
			public int Width { get { return (int)Graphic.Width; } }
			public int Height { get { return (int)Graphic.Height; } }
			public ChildCollection Children { get; protected set; }
			public XmlGraphic Graphic { get; set; }
			public Element Parent { get; set; }

			#endregion

			static readonly Dictionary<string, Type> ElementTypes = new Dictionary<string, Type>
			{
				{ "Rectangle",	typeof(XmlRectangle) },
				{ "TextBlock",	typeof(XmlTextBlock) },
				{ "HotSpot",	typeof(XmlHotSpot) },
				{ "Image",		typeof(XmlRectangle) },
			};

			public void SetAttribute(string Attr, string Value)
			{
				Graphic?.SetAttribute(Attr, Value);

				// Assign our own properties
				switch (Attr)
				{
					case "Name":
						Name = Value;
						break;

					case "Top":
						Top = int.Parse(Value);
						break;

					case "Left":
						Left = int.Parse(Value);
						break;
				}
			}

			public void Draw(ContainerVisual DrawList, int x, int y)
			{
				x += Left;
				y += Top;
				Graphic.Draw(DrawList, x, y);
				foreach (Element el in Children)
					el.Draw(DrawList, x, y);
			}
		}

		internal class NamedBitmap
		{
			public NamedBitmap(string name, int width, int height)
			{
				Name = name;
				Height = height;
				Width = width;
				Bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
			}
			public string Name { get; set; }
			public int Height { get; set; }
			public int Width { get; set; }
			public RenderTargetBitmap Bitmap { get; set; }
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

		Dictionary<string, Element> Components;
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
			ContainerVisual DrawList;
			NamedBitmap bmp;

			el = BuildElement(node);
			DrawList = new ContainerVisual();
			el.Draw(DrawList, 0, 0);
			bmp = new NamedBitmap(el.Name, el.Width, el.Height);
			bmp.Bitmap.Render(DrawList);
			Images.Add(bmp);
		}

		Element BuildElement(XmlNode node, Element parent = null)
		{
			Element el;

			el = new Element(node.Name, parent);
			foreach (XmlAttribute attr in node.Attributes)
				el.SetAttribute(attr.Name, attr.Value);

			foreach (XmlNode nodeChild in node.ChildNodes)
				el.Children.Add(BuildElement(nodeChild, el));

			return el;
		}

		#endregion
	}
}
