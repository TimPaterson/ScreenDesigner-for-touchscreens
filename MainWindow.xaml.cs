﻿using PatersonTech;
using ScreenDesigner.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Xml;

namespace ScreenDesigner
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		const string StrXmlFileFilter = "XML Files|*.xml|All Files|*.*";
		const int GroupExtraHeight = 30;
		const int GroupExtraWidth = 20;

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
		}

		static T Clone<T>(T obj) where T : Visual
		{
			string xml = XamlWriter.Save(obj);
			var xmlTextReader = new XmlTextReader(new StringReader(xml));
			var deepCopyObject = (T)XamlReader.Load(xmlTextReader);
			return deepCopyObject;
		}


		#region Private Fields

		FileSystemWatcher m_watcher;
		Dispatcher m_dispatcher;
		List<XmlImage.NamedBitmap> m_Images;

		#endregion


		#region Private Methods

		bool LoadXml(string strXmlFileName)
		{
			XmlDocument doc;
			XmlImage parser;

			doc = new XmlDocument();
			try
			{
				doc.Load(strXmlFileName);
			}
			catch (XmlException exc)
			{
				// UNDONE: XML parsing error
				Debug.WriteLine(exc.Message);
				return true;	// file exists, remember it
			}
			catch (Exception exc)
			{
				Debug.WriteLine(exc.Message);
				return false;
			}

			try
			{
				parser = new XmlImage();
				m_Images = parser.ParseXml(doc);
				StartImageDisplay();
				foreach (XmlImage.NamedBitmap bmp in m_Images)
					DisplayImage(bmp);
				EndImageDisplay();
			}
			catch (Exception exc)
			{
				// UNDONE: parsing error
				Debug.WriteLine(exc.Message);
				Debug.WriteLine(exc.InnerException.Message);
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

		void DisplayImage(XmlImage.NamedBitmap bmp)
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
			m_dispatcher.InvokeAsync(LoadXmlDefault);
		}

		void Watcher_OnRenamed(object source, RenamedEventArgs e)
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

			path = Path.GetDirectoryName(Settings.Default.XmlFileName);

			foreach (XmlImage.NamedBitmap bmp in m_Images)
			{
				BmpBitmapEncoder encode = new BmpBitmapEncoder();
				encode.Frames.Add(BitmapFrame.Create(bmp.Bitmap));
				filename = Path.Combine(path, bmp.Name) + ".bmp";
				using (Stream stream = File.Open(filename, FileMode.Create))
					encode.Save(stream);
			}
		}

		private void btnBrowse_Click(object sender, RoutedEventArgs e)
		{

		}

		#endregion
	}
}
