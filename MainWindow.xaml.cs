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
using static ScreenDesigner.ColorOperations;

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
		const int GroupExtraHeight = 25;
		const int GroupExtraWidth = 12;

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
		List<KeyValuePair<string, int>> m_Colors;
		List<ShowValue> m_Values;
		Timer m_timer;

		#endregion


		#region Private Methods

		bool LoadXml(string strXmlFileName)
		{
			XDocument doc;
			XmlScreen parser;
			XmlReader reader;
			XmlReaderSettings settings;
			FileStream stream;
			int maxWidth;
			string strFolder;

			Title = StrTitle + " - " + Path.GetFileName(strXmlFileName);
			txtSaved.Visibility = Visibility.Hidden;
			stream = null;

			try
			{
				stream = new FileStream(strXmlFileName, FileMode.Open);
				settings = new XmlReaderSettings();
				settings.XmlResolver = new XmlUrlResolver();
				settings.DtdProcessing = DtdProcessing.Parse;
				strFolder = Path.GetDirectoryName(strXmlFileName) + '\\';
				reader = XmlReader.Create(stream, settings, strFolder);
				doc = XDocument.Load(reader, LoadOptions.SetLineInfo);
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
			finally
			{
				stream?.Close();
			}

			try
			{
				parser = new XmlScreen();
				m_Images = parser.ParseXml(doc, strFolder);
				m_Colors = parser.GetColors();
				m_Values = parser.GetValues();
				maxWidth = 0;
				foreach (NamedBitmap bmp in m_Images)
					maxWidth = Math.Max(maxWidth, bmp.Width);
				StartImageDisplay(maxWidth);
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

		void StartImageDisplay(int maxWidth)
		{
			pnlImages.Children.Clear();
			pnlImages.Width = maxWidth + GroupExtraWidth + 2;
		}

		void EndImageDisplay()
		{
			double maxHeight;

			maxHeight = Height;
			SizeToContent = SizeToContent.WidthAndHeight;
			SizeToContent = SizeToContent.Manual;
			pnlImages.Width = double.NaN;
			if (Height > maxHeight)
				Height = maxHeight;
		}

		void DisplayImage(NamedBitmap bmp)
		{
			GroupBox group;
			Border border;
			Image image;

			CondenseColors(bmp);

			image = new Image();
			image.Width = bmp.Width;
			image.Height = bmp.Height;
			image.Source = bmp.Bitmap;

			border = new Border();
			border.BorderThickness = new Thickness(1);
			border.BorderBrush = new SolidColorBrush(Colors.Black);
			border.Width = bmp.Width + 2;
			border.Height = bmp.Height + 2;
			border.Child = image;

			group = new GroupBox();
			group.Header = bmp.Name;
			group.Height = border.Height + GroupExtraHeight;
			group.Content = border;
			group.VerticalAlignment = VerticalAlignment.Top;

			pnlImages.Children.Add(group);
		}

		void CondenseColors(NamedBitmap bmp)
		{
			byte[] arPx32;

			if (bmp.BytesPerPixel == 3)
				return;

			arPx32 = new byte[bmp.Height * bmp.Width * 4];
			bmp.Bitmap.CopyPixels(arPx32, bmp.Width * 4, 0);

			if (bmp.BytesPerPixel == 1)
			{
				// Reduce color resolution to 3:3:2 red:green:blue
				for (int i = 0; i < arPx32.Length; i += 4)
				{
					arPx32[i] = LimitColor(arPx32[i], BpC.B8);
					arPx32[i + 1] = LimitColor(arPx32[i + 1], BpC.G8);
					arPx32[i + 2] = LimitColor(arPx32[i + 2], BpC.R8);
				}
			}
			else
			{
				// Reduce color resolution to 5:6:5 red:green:blue
				for (int i = 0; i < arPx32.Length; i += 4)
				{
					arPx32[i] = LimitColor(arPx32[i], BpC.B16);
					arPx32[i + 1] = LimitColor(arPx32[i + 1], BpC.G16);
					arPx32[i + 2] = LimitColor(arPx32[i + 2], BpC.R16);
				}
			}
			bmp.Bitmap = BitmapSource.Create(bmp.Width, bmp.Height, 96, 96, PixelFormats.Pbgra32, null, arPx32, bmp.Width * 4);
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
			FileGenerator output;
			string path;

			output = new FileGenerator();

			if (Settings.Default.OutputFileFolder != null)
				path = Settings.Default.OutputFileFolder;
			else
				path = Path.GetDirectoryName(Settings.Default.XmlFileName);

			path = Path.Combine(path, Path.GetFileNameWithoutExtension(Settings.Default.XmlFileName));
			output.Open(path + ".h", path + ".bin");

			output.WriteColors(m_Colors);
			output.WriteValues(m_Values);

			// Ouput each image as .bmp and associated .h file with hotspots and locations
			foreach (NamedBitmap bmp in m_Images)
				output.WriteImage(bmp);

			output.Close();
			txtSaved.Visibility = Visibility.Visible;
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
