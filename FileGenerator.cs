using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ScreenDesigner
{
	class FileGenerator
	{
		const string StrMacroUndef = "#undef {0}";
		const string StrMacroPredfine1 =
@"#ifndef {0}
#define {0}(a)
#endif";
		const string StrMacroPredfine2 =
@"#ifndef {0}
#define {0}(a,b)
#endif";
		const string StrMacroPredfine3 =
@"#ifndef {0}
#define {0}(a,b,c)
#endif";
		const string StrMacroPredfine6 =
@"#ifndef {0}
#define {0}(a,b,c,d,e,f)
#endif";

		// Overall screen defintion
		const string StrStartScreen		= "START_SCREEN";
		const string StrImageAddress	= "IMAGE_ADDRESS";
		const string StrImageSize		= "IMAGE_SIZE";
		const string StrImageWidth		= "IMAGE_WIDTH";
		const string StrImageHeight		= "IMAGE_HEIGHT";
		const string StrImageDepth		= "IMAGE_DEPTH";
		const string StrEndScreen		= "END_SCREEN";

		const string StrStartHotspots	= "START_HOTSPOTS";
		const string StrHotspot			= "DEFINE_HOTSPOT";
		const string StrEndHotspots		= "END_HOTSPOTS";
		const string StrCountHotSpots	= "HOTSPOT_COUNT";

		const string StrStartLocations	= "START_LOCATIONS";
		const string StrLocation		= "DEFINE_LOCATION";
		const string StrEndLocations	= "END_LOCATIONS";

		const string StrStartGroup		= "START_GROUP";
		const string StrGroupHotspot	= "GROUP_HOTSPOT";
		const string StrEndGroup		= "END_GROUP";

		const string StrFileLength		= "SCREEN_FILE_LENGTH";

		StreamWriter Writer;
		FileStream Stream;
		int Offset;
		Dictionary<string, List<HotSpot>> groups;

		public void Open(string fileNameHeader, string fileNameBinary)
		{
			if (fileNameHeader != null)
			{
				Writer = new StreamWriter(fileNameHeader);
				Offset = 0;
				groups = new Dictionary<string, List<HotSpot>>();

				Predefine1(StrStartScreen);
				Predefine1(StrImageAddress);
				Predefine1(StrImageSize);
				Predefine1(StrImageWidth);
				Predefine1(StrImageHeight);
				Predefine1(StrImageDepth);
				Predefine1(StrEndScreen);

				Predefine1(StrStartHotspots);
				Predefine6(StrHotspot);
				Predefine1(StrEndHotspots);
				Predefine2(StrCountHotSpots);

				Predefine1(StrStartLocations);
				Predefine3(StrLocation);
				Predefine1(StrEndLocations);

				Predefine1(StrStartGroup);
				Predefine6(StrGroupHotspot);
				Predefine1(StrEndGroup);

				Predefine1(StrFileLength);
			}

			if (fileNameBinary != null)
			{
				Stream = new FileStream(fileNameBinary, FileMode.Create);
			}
		}

		public void Close()
		{
			if (Writer != null)
			{
				// Now output them by group
				if (groups.Count > 1)
				{
					foreach (string group in groups.Keys)
					{
						Writer.WriteLine();
						DefineHead(StrStartGroup, group);
						foreach (HotSpot spot in groups[group])
						{
							Writer.WriteLine("\t" + StrGroupHotspot + "({0}, {1}, {2}, {3}, {4}, {5})",
								spot.Name, spot.Group, spot.MinX, spot.MinY, spot.MaxX, spot.MaxY);
						}
						DefineHead(StrEndGroup, group);
					}

					// Do them again, but with a unique macro for each group
					foreach (string group in groups.Keys)
					{
						Writer.WriteLine();
						Predefine1(StrStartGroup + "_" + group);
						Predefine6(StrGroupHotspot + "_" + group);
						Predefine1(StrEndGroup + "_" + group);

						Writer.WriteLine();
						DefineHead(StrStartGroup + "_" + group, group);
						foreach (HotSpot spot in groups[group])
						{
							Writer.WriteLine("\t" + StrGroupHotspot + "_" + group + "({0}, {1}, {2}, {3}, {4}, {5})",
								spot.Name, spot.Group, spot.MinX, spot.MinY, spot.MaxX, spot.MaxY);
						}
						DefineHead(StrEndGroup + "_" + group, group);

						Writer.WriteLine();
						Undefine(StrStartGroup + "_" + group);
						Undefine(StrGroupHotspot + "_" + group);
						Undefine(StrEndGroup + "_" + group);
					}
				}

				Writer.WriteLine();
				DefineHead(StrFileLength, Offset);

				Writer.WriteLine();
				Undefine(StrStartScreen);
				Undefine(StrImageAddress);
				Undefine(StrImageSize);
				Undefine(StrImageWidth);
				Undefine(StrImageHeight);
				Undefine(StrImageDepth);
				Undefine(StrEndScreen);

				Undefine(StrStartHotspots);
				Undefine(StrHotspot);
				Undefine(StrEndHotspots);
				Undefine(StrCountHotSpots);

				Undefine(StrStartLocations);
				Undefine(StrLocation);
				Undefine(StrEndLocations);

				Undefine(StrStartGroup);
				Undefine(StrGroupHotspot);
				Undefine(StrEndGroup);

				Undefine(StrFileLength);

				Writer.Close();
				Writer = null;
			}

			if (Stream != null)
			{
				Stream.Close();
				Stream = null;
			}
		}

		public void WriteImage(NamedBitmap bmp)
		{
			int bitmapSize;

			bitmapSize = bmp.Height * bmp.Width * bmp.BytesPerPixel;

			if (Writer != null)
			{
				Writer.WriteLine();
				DefineHead(StrStartScreen, bmp.Name);
				DefineValue(StrImageAddress, Offset);
				DefineValue(StrImageSize, bitmapSize);
				DefineValue(StrImageWidth, bmp.Width);
				DefineValue(StrImageHeight, bmp.Height);
				DefineValue(StrImageDepth, bmp.ColorDepth);
				DefineHead(StrEndScreen, bmp.Name);

				if (bmp.HotSpots.Count != 0)
				{
					Writer.WriteLine();
					DefineHead(StrStartHotspots, bmp.Name);
					foreach (HotSpot spot in bmp.HotSpots)
					{
						Writer.WriteLine("\t" + StrHotspot + "({0}, {1}, {2}, {3}, {4}, {5})",
							spot.Name, spot.Group, spot.MinX, spot.MinY, spot.MaxX, spot.MaxY);
						if (string.IsNullOrEmpty(spot.Group))
							spot.Group = "";    // make sure not null

						if (!groups.ContainsKey(spot.Group))
							groups.Add(spot.Group, new List<HotSpot>());

						groups[spot.Group].Add(spot);
					}
					DefineHead(StrEndHotspots, bmp.Name);

					Writer.WriteLine();
					DefineNamedValue(StrCountHotSpots, bmp.Name, bmp.HotSpots.Count);
				}

				if (bmp.Locations.Count != 0)
				{
					Writer.WriteLine();
					DefineHead(StrStartLocations, bmp.Name);
					foreach (Location loc in bmp.Locations)
						Writer.WriteLine("\t" + StrLocation + "({0}, {1}, {2})", loc.Name, loc.X, loc.Y);
					DefineHead(StrEndLocations, bmp.Name);
				}

				Offset += bitmapSize;
			}

			if (Stream != null)
			{
				byte[] arPx;

				arPx = ConvertColorBitmap(bmp);
				Stream.Write(arPx, 0, arPx.Length);
			}
		}

		byte[] ConvertColorBitmap(NamedBitmap bmp)
		{
			byte[] arPx, arPx32;
			int size;
			int pixel;

			size = bmp.Height * bmp.Width;
			arPx = new byte[size * bmp.BytesPerPixel];
			arPx32 = new byte[size * 4];
			bmp.Bitmap.CopyPixels(arPx32, bmp.Width * 4, 0);

			switch (bmp.BytesPerPixel)
			{
				case 1:
					// Convert from 8 bits per pixel to 3:3:2 red:green:blue. 
					// Color values have already been rounded.
					for (int i = 0, j = 0; i < arPx32.Length; i += 4, j++)
					{
						pixel = arPx32[i] >> 6;					// 2 bits of blue
						pixel |= (arPx32[i + 1] & 0xE0) >> 3;	// 3 bits of green
						pixel |= arPx32[i + 2] & 0xE0;			// 3 bits of red
						arPx[j] = (byte)pixel;
					}
					break;

				case 2:
					// Convert from 8 bits per pixel to 5:6:5 red:green:blue. 
					// Color values have already been rounded.
					for (int i = 0, j = 0; i < arPx32.Length; i += 4, j += 2)
					{
						pixel = arPx32[i] >> 3;						// 5 bits of blue
						pixel |= (int)(arPx32[i + 1] & 0xFC) << 3;	// 6 bits of green
						pixel |= (int)(arPx32[i + 2] & 0xF8) << 8;	// 5 bits of red
						arPx[j] = (byte)pixel;
						arPx[j + 1] = (byte)(pixel >> 8);
					}
					break;

				case 3:
					// No conversion necessary, just skip over alpha byte
					for (int i = 0, j = 0; i < arPx32.Length; i += 4, j += 3)
					{
						arPx[j] = arPx32[i];
						arPx[j + 1] = arPx32[i + 1];
						arPx[j + 2] = arPx32[i + 2];
					}
					break;
			}

			return arPx;
		}

		void Predefine1(string macro)
		{
			Writer.WriteLine(StrMacroPredfine1, macro);
		}

		void Predefine2(string macro)
		{
			Writer.WriteLine(StrMacroPredfine2, macro);
		}

		void Predefine3(string macro)
		{
			Writer.WriteLine(StrMacroPredfine3, macro);
		}

		void Predefine6(string macro)
		{
			Writer.WriteLine(StrMacroPredfine6, macro);
		}

		void DefineHead(string macro, object value)
		{
			Writer.WriteLine(macro + "({0})", value);
		}

		void DefineValue(string macro, object value)
		{
			Writer.WriteLine("\t" + macro + "({0})", value);
		}

		void DefineNamedValue(string macro, string name, object value)
		{
			Writer.WriteLine(macro + "({0}, {1})", name, value);
		}

		void Undefine(string macro)
		{
			Writer.WriteLine(StrMacroUndef, macro);
		}
	}
}
