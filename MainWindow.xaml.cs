using PatersonTech;
using ScreenDesigner.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;

namespace ScreenDesigner
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public const string StrTitle = "Screen Designer";
		const string StrXmlSchemaPath = @"..\..\ScreenDesigner.xsd";
		const string StrXmlFileFilter = "XML Files|*.xml|All Files|*.*";
		const int GroupExtraHeight = 30;
		const int GroupExtraWidth = 20;

		// Output file
		const string StrStartOutput = "// Locations and Hotspots";
		const string StrMacroUndef = "#undef {0}";
		// Locations
		const string StrLocationMacroPredfine = 
@"#ifndef {0}
#define {0}(a,b,c)
#endif";
		const string StrStartLocations = "// Locations";
		const string StrLocationMacro = "DEFINE_LOCATION";
		const string StrDefineLocation = StrLocationMacro + "({0}, {1}, {2})";
		// Hotspots
		const string StrHotspotMacroPredfine = 
@"#ifndef {0}
#define {0}(a,b,c,d,e,f)
#endif";
		const string StrStartHotspots = "// Hotspots";
		const string StrHotspotMacro = "DEFINE_HOTSPOT";
		const string StrDefineHotspot = StrHotspotMacro + "({0}, {1}, {2}, {3}, {4}, {5})";
		// Hotspot groups
		const string StrGroupStartEndPredfine = 
@"#ifndef {0}
#define {0}(a)
#endif";
		const string StrStartGroupMacro = "START_GROUP";
		const string StrDefineGroupStart = StrStartGroupMacro + "({0})";
		const string StrEndGroupMacro = "END_GROUP";
		const string StrDefineGroupEnd = StrEndGroupMacro + "({0})";
		const string StrGroupMacroPredfine = 
@"#ifndef {0}_{1}
#define {0}_{1}(a,b,c,d,e,f)
#endif";
		const string StrDefineHotspotGroup = StrHotspotMacro + "_{1}({0}, {1}, {2}, {3}, {4}, {5})";
		const string StrGroupUndef = "#undef {0}_{1}";

		public MainWindow()
		{
			InitializeComponent();

			// Set up file watcher to detect if loaded file changes
			m_watcher = new FileSystemWatcher();
			m_watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName;
			m_watcher.Changed += Watcher_OnChanged;
			m_watcher.Created += Watcher_OnChanged;
			m_watcher.Deleted += Watcher_OnChanged;
			m_watcher.Renamed += Watcher_OnRenamed;
			m_dispatcher = Dispatcher.CurrentDispatcher;
			m_timer = new Timer(10);    // Delay 10 ms after file change event
			m_timer.AutoReset = false;
			m_timer.Elapsed += Timer_OnElapsed;
		}


		#region Private Fields

		FileSystemWatcher m_watcher;
		Dispatcher m_dispatcher;
		List<NamedBitmap> m_Images;
		Timer m_timer;

		#endregion


		#region Private Methods

		bool LoadXml(string strXmlFileName)
		{
			XDocument doc;
			XmlScreen parser;

			Title = StrTitle + " - " + Path.GetFileName(strXmlFileName);

			try
			{
				doc = XDocument.Load(strXmlFileName, LoadOptions.SetLineInfo);
				XmlSchemaSet schemas = new XmlSchemaSet();
				schemas.Add(null, StrXmlSchemaPath);
				doc.Validate(schemas, null);
			}
			catch (XmlException exc)
			{
				MessageBox.Show($"XML Error:\n{exc.Message}", 
					StrTitle, MessageBoxButton.OK, MessageBoxImage.Error);
				pnlImages.Children.Clear();
				return true;	// file exists, remember it
			}
			catch (XmlSchemaValidationException exc)
			{
				MessageBox.Show($"XML Error at line {exc.LineNumber}:\n{exc.Message}", 
					StrTitle, MessageBoxButton.OK, MessageBoxImage.Error);
				pnlImages.Children.Clear();
				return true;	// file exists, remember it
			}
			catch (Exception exc)
			{
				MessageBox.Show($"Error:\n{exc.Message}", StrTitle, MessageBoxButton.OK, MessageBoxImage.Error);
				pnlImages.Children.Clear();
				return false;
			}

			try
			{
				parser = new XmlScreen();
				m_Images = parser.ParseXml(doc);
				StartImageDisplay();
				foreach (NamedBitmap bmp in m_Images)
					DisplayImage(bmp);
				EndImageDisplay();
			}
			catch (CaughtException exc)
			{
				MessageBox.Show(exc.Message, StrTitle, MessageBoxButton.OK, MessageBoxImage.Error);
				pnlImages.Children.Clear();
			}
			catch (Exception exc)
			{
				MessageBox.Show($"Error:\n{exc.Message}", StrTitle, MessageBoxButton.OK, MessageBoxImage.Error);
				pnlImages.Children.Clear();
			}
			return true;
		}

		bool LoadNewFile(string strXmlFileName)
		{
			if (LoadXml(strXmlFileName))
			{
				Settings.Default.XmlFileName = strXmlFileName;
				m_watcher.Path = System.IO.Path.GetDirectoryName(Settings.Default.XmlFileName);
				m_watcher.Filter = System.IO.Path.GetFileName(Settings.Default.XmlFileName);
				m_watcher.EnableRaisingEvents = true;
				return true;
			}

			m_watcher.EnableRaisingEvents = false;
			return false;
		}

		void LoadXmlDefault()
		{
			LoadXml(Settings.Default.XmlFileName);
		}

		void StartImageDisplay()
		{
			pnlImages.Children.Clear();
		}

		void EndImageDisplay()
		{
			SizeToContent = SizeToContent.WidthAndHeight;
		}

		void DisplayImage(NamedBitmap bmp)
		{
			GroupBox group;
			Border border;
			Image image;

			image = new Image();
			image.Width = bmp.Width;
			image.Height = bmp.Height;
			image.Source = bmp.Bitmap;

			border = new Border();
			border.BorderThickness = new Thickness(1);
			border.BorderBrush = new SolidColorBrush(Colors.Black);
			border.Width = bmp.Width;
			border.Height = bmp.Height;
			border.Child = image;

			group = new GroupBox();
			group.Header = bmp.Name;
			group.Width = bmp.Width + GroupExtraWidth;
			group.Height = bmp.Height + GroupExtraHeight;
			group.Content = border;
			group.HorizontalAlignment = HorizontalAlignment.Left;

			pnlImages.Children.Add(group);
		}

		#endregion


		#region Event Handlers

		void Watcher_OnChanged(object source, FileSystemEventArgs e)
		{
			m_timer.Stop();
			m_timer.Start();
		}

		void Watcher_OnRenamed(object source, RenamedEventArgs e)
		{
			m_timer.Stop();
			m_timer.Start();
		}

		void Timer_OnElapsed(object source, ElapsedEventArgs e)
		{
			m_dispatcher.InvokeAsync(LoadXmlDefault);
		}

		private void Window_Initialized(object sender, EventArgs e)
		{
			// Read in saved per-user settings
			if (!Settings.Default.SettingsUpgraded)
			{
				Settings.Default.Upgrade();
				Settings.Default.SettingsUpgraded = true;
				Settings.Default.Save();
			}
		}

		protected override void OnSourceInitialized(EventArgs e)
		{
			// Initialized event is too early to set window placement.
			// Loaded event has already drawn the window, so if we 
			// wait that long we see it move.
			base.OnSourceInitialized(e);
			this.SetPlacement(Settings.Default.MainWindowPlacement);
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			LoadNewFile(Settings.Default.XmlFileName);
		}

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			Settings.Default.MainWindowPlacement = this.GetPlacement();
			Settings.Default.Save();
		}

		private void btnLoadXml_Click(object sender, RoutedEventArgs e)
		{
			Microsoft.Win32.OpenFileDialog dlg;

			dlg = new Microsoft.Win32.OpenFileDialog();
			dlg.Filter = StrXmlFileFilter;

			try
			{
				dlg.InitialDirectory = System.IO.Path.GetDirectoryName(Settings.Default.XmlFileName);
				dlg.FileName = System.IO.Path.GetFileName(Settings.Default.XmlFileName);
			}
			catch { }

			if (dlg.ShowDialog(this) == true)
			{
				if (!LoadNewFile(dlg.FileName))
				{
					// UNDONE: file error
				}
			}
		}

		private void btnSave_Click(object sender, RoutedEventArgs e)
		{
			string path;
			string filename;
			HashSet<string> groups;
			bool fNoGroup;

			if (Settings.Default.OutputFileFolder != null)
				path = Settings.Default.OutputFileFolder;
			else
				path = Path.GetDirectoryName(Settings.Default.XmlFileName);

			ColorContext clrSrc = new ColorContext(PixelFormats.Pbgra32);
			ColorContext clrDst = new ColorContext(PixelFormats.Bgr565);

			// Ouput each image as .bmp and associated .h file with hotspots and locations
			foreach (NamedBitmap bmp in m_Images)
			{
				ColorConvertedBitmap cvt;
				byte[] arPx = new byte[bmp.Height * bmp.Width * 2];

				cvt = new ColorConvertedBitmap(bmp.Bitmap, clrSrc, clrDst, PixelFormats.Bgr565);
				cvt.CopyPixels(arPx, bmp.Width * 2, 0);

				// Output bitmap image
				if (bmp.Folder != null)
					filename = Path.Combine(path, bmp.Folder);
				else
					filename = path;
				Directory.CreateDirectory(filename);
				filename = Path.Combine(filename, bmp.Name);
				File.WriteAllBytes(filename + ".bin", arPx);

				if (bmp.Locations.Count != 0 || bmp.HotSpots.Count != 0)
				{
					// Output .h file
					using (StreamWriter writer = new StreamWriter(filename + ".h"))
					{
						groups = new HashSet<string>();
						fNoGroup = false;
						writer.WriteLine(StrStartOutput);

						if (bmp.Locations.Count != 0)
						{
							writer.WriteLine(StrStartLocations);
							writer.WriteLine(StrLocationMacroPredfine, StrLocationMacro);
							writer.WriteLine();
							foreach (Location loc in bmp.Locations)
								writer.WriteLine(StrDefineLocation, loc.Name, loc.X, loc.Y);
							writer.WriteLine();
						}

						if (bmp.HotSpots.Count != 0)
						{
							// Firset output all hotspots, regardess of group
							writer.WriteLine(StrStartHotspots);
							writer.WriteLine(StrHotspotMacroPredfine, StrHotspotMacro);
							writer.WriteLine();
							foreach (HotSpot spot in bmp.HotSpots)
							{
								writer.WriteLine(StrDefineHotspot, spot.Name, spot.Group, spot.MinX, spot.MinY, spot.MaxX, spot.MaxY);
								if (string.IsNullOrEmpty(spot.Group))
								{
									fNoGroup = true;
									spot.Group = "";	// make sure not null
								}
								else
									groups.Add(spot.Group);
							}
							writer.WriteLine();

							// Now output them by group
							if (groups.Count != 0)
							{
								if (fNoGroup)
									groups.Add("");
								writer.WriteLine(StrGroupStartEndPredfine, StrStartGroupMacro);
								writer.WriteLine(StrGroupStartEndPredfine, StrEndGroupMacro);
							}
							foreach (string group in groups)
							{
								writer.WriteLine(StrGroupMacroPredfine, StrHotspotMacro, group);
								writer.WriteLine();
								writer.WriteLine(StrDefineGroupStart, group);
								foreach (HotSpot spot in bmp.HotSpots)
								{
									if (spot.Group == group)
										writer.WriteLine(StrDefineHotspotGroup, spot.Name, spot.Group, spot.MinX, spot.MinY, spot.MaxX, spot.MaxY);
								}
								writer.WriteLine(StrDefineGroupEnd, group);
								writer.WriteLine();
							}
						}

						if (bmp.Locations.Count != 0)
							writer.WriteLine(StrMacroUndef, StrLocationMacro);

						if (bmp.HotSpots.Count != 0)
						{
							writer.WriteLine(StrMacroUndef, StrHotspotMacro);
							foreach (string group in groups)
								writer.WriteLine(StrGroupUndef, StrHotspotMacro, group);
							if (groups.Count != 0)
							{
								writer.WriteLine(StrMacroUndef, StrStartGroupMacro);
								writer.WriteLine(StrMacroUndef, StrEndGroupMacro);
							}
						}
					}
				}
			}
		}

		private void btnBrowse_Click(object sender, RoutedEventArgs e)
		{
			string path;
			Ookii.Dialogs.Wpf.VistaFolderBrowserDialog dlg;

			if (Settings.Default.OutputFileFolder != null)
				path = Settings.Default.OutputFileFolder;
			else
				path = Path.GetDirectoryName(Settings.Default.XmlFileName);

			dlg = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog();
			dlg.SelectedPath = path;
			if (dlg.ShowDialog(this) == true)
			{
				Settings.Default.OutputFileFolder = dlg.SelectedPath;
			}
		}

		#endregion
	}
}
