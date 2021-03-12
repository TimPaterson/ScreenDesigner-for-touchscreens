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
		const string StrMacroPredfine =
@"#ifndef {0}
#define {0}({1})
#endif";

		// Overall screen defintion
		const string StrStartScreen		= "START_SCREEN";
		const string StrImageAddress	= "IMAGE_ADDRESS";
		const string StrImageSize		= "IMAGE_SIZE";
		const string StrImageWidth		= "IMAGE_WIDTH";
		const string StrImageHeight		= "IMAGE_HEIGHT";
		const string StrImageStride		= "IMAGE_STRIDE";
		const string StrImageDepth		= "IMAGE_DEPTH";
		const string StrEndScreen		= "END_SCREEN";

		const string StrStartHotspots	= "START_HOTSPOTS";
		const string StrHotspot			= "DEFINE_HOTSPOT";
		const string StrEndHotspots		= "END_HOTSPOTS";
		const string StrCountHotSpots	= "HOTSPOT_COUNT";

		const string StrStartLocations	= "START_LOCATIONS";
		const string StrLocation		= "DEFINE_LOCATION";
		const string StrEndLocations	= "END_LOCATIONS";

		const string StrStartAreas		= "START_AREAS";
		const string StrArea			= "DEFINE_AREA";
		const string StrEndAreas		= "END_AREAS";

		const string StrStartGroup		= "START_GROUP";
		const string StrGroupHotspot	= "GROUP_HOTSPOT";
		const string StrEndGroup		= "END_GROUP";

		const string StrStartValues		= "START_VALUES";
		const string StrValue			= "DEFINE_VALUE";
		const string StrEndValues		= "END_VALUES";

		const string StrStartStringValues	= "START_STR_VALUES";
		const string StrStringValue			= "DEFINE_STR_VALUE";
		const string StrEndStringValues		= "END_STR_VALUES";

		const string StrFileLength		= "SCREEN_FILE_LENGTH";

		const string StrStartColors		= "START_COLORS";
		const string StrColor			= "DEFINE_COLOR";
		const string StrEndColors		= "END_COLORS";

		class BitmapInfo
		{
			public BitmapInfo(NamedBitmap bmp, int offset)
			{
				Bmp = bmp;
				Offset = offset;
			}

			public NamedBitmap Bmp;
			public int Offset;
		}

		StreamWriter Writer;
		FileStream Stream;
		int Offset;
		Dictionary<string, List<HotSpot>> Groups;
		Dictionary<string, List<BitmapInfo>> Types;

		public void Open(string fileNameHeader, string fileNameBinary)
		{
			if (fileNameHeader != null)
			{
				Writer = new StreamWriter(fileNameHeader);
				Offset = 0;
				Groups = new Dictionary<string, List<HotSpot>>();
				Types = new Dictionary<string, List<BitmapInfo>>();

				Predefine1(StrStartScreen);
				Predefine1(StrImageAddress);
				Predefine1(StrImageSize);
				Predefine1(StrImageWidth);
				Predefine1(StrImageHeight);
				Predefine1(StrImageStride);
				Predefine1(StrImageDepth);
				Predefine1(StrEndScreen);

				Predefine1(StrStartHotspots);
				Predefine6(StrHotspot);
				Predefine1(StrEndHotspots);
				Predefine2(StrCountHotSpots);

				Predefine1(StrStartLocations);
				Predefine3(StrLocation);
				Predefine1(StrEndLocations);

				Predefine1(StrStartAreas);
				Predefine5(StrArea);
				Predefine1(StrEndAreas);

				Predefine1(StrStartGroup);
				Predefine6(StrGroupHotspot);
				Predefine1(StrEndGroup);

				Predefine0(StrStartValues);
				Predefine2(StrValue);
				Predefine0(StrEndValues);

				Predefine0(StrStartStringValues);
				Predefine2(StrStringValue);
				Predefine0(StrEndStringValues);

				Predefine0(StrStartColors);
				Predefine2(StrColor);
				Predefine0(StrEndColors);

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
				if (Groups.Count > 1)
				{
					foreach (string group in Groups.Keys)
					{
						Writer.WriteLine();
						DefineHead(StrStartGroup, group);
						foreach (HotSpot spot in Groups[group])
						{
							Writer.WriteLine("\t" + StrGroupHotspot + "({0}, {1}, {2}, {3}, {4}, {5})",
								spot.Name, spot.Group, spot.MinX, spot.MinY, spot.MaxX, spot.MaxY);
						}
						DefineHead(StrEndGroup, group);
					}

					// Do them again, but with a unique macro for each group
					foreach (string group in Groups.Keys)
					{
						Writer.WriteLine();
						Predefine1(StrStartGroup + "_" + group);
						Predefine6(StrGroupHotspot + "_" + group);
						Predefine1(StrEndGroup + "_" + group);

						Writer.WriteLine();
						DefineHead(StrStartGroup + "_" + group, group);
						foreach (HotSpot spot in Groups[group])
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

				// Now output all bitmaps that have a type
				if (Types.Count != 0)
				{
					foreach (string type in Types.Keys)
					{
						Writer.WriteLine();
						Predefine1(StrStartScreen + "_" + type);
						Predefine1(StrImageAddress + "_" + type);
						Predefine1(StrImageSize + "_" + type);
						Predefine1(StrImageWidth + "_" + type);
						Predefine1(StrImageHeight + "_" + type);
						Predefine1(StrImageStride + "_" + type);
						Predefine1(StrImageDepth + "_" + type);
						Predefine1(StrEndScreen + "_" + type);

						foreach (BitmapInfo info in Types[type])
						{
							NamedBitmap bmp = info.Bmp;
							Writer.WriteLine();
							DefineHead(StrStartScreen + "_" + type, bmp.Name);
							DefineValue(StrImageAddress + "_" + type, info.Offset);
							DefineValue(StrImageSize + "_" + type, bmp.Height * bmp.Stride * bmp.BytesPerPixel);
							DefineValue(StrImageWidth + "_" + type, bmp.Width);
							DefineValue(StrImageHeight + "_" + type, bmp.Height);
							DefineValue(StrImageStride + "_" + type, bmp.Stride);
							DefineValue(StrImageDepth + "_" + type, bmp.ColorDepth);
							DefineHead(StrEndScreen + "_" + type, bmp.Name);
						}

						Writer.WriteLine();
						Undefine(StrStartScreen + "_" + type);
						Undefine(StrImageAddress + "_" + type);
						Undefine(StrImageSize + "_" + type);
						Undefine(StrImageWidth + "_" + type);
						Undefine(StrImageHeight + "_" + type);
						Undefine(StrImageStride + "_" + type);
						Undefine(StrImageDepth + "_" + type);
						Undefine(StrEndScreen + "_" + type);

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
				Undefine(StrImageStride);
				Undefine(StrImageDepth);
				Undefine(StrEndScreen);

				Undefine(StrStartHotspots);
				Undefine(StrHotspot);
				Undefine(StrEndHotspots);
				Undefine(StrCountHotSpots);

				Undefine(StrStartLocations);
				Undefine(StrLocation);
				Undefine(StrEndLocations);

				Undefine(StrStartAreas);
				Undefine(StrArea);
				Undefine(StrEndAreas);

				Undefine(StrStartGroup);
				Undefine(StrGroupHotspot);
				Undefine(StrEndGroup);

				Undefine(StrStartValues);
				Undefine(StrValue);
				Undefine(StrEndValues);

				Undefine(StrStartStringValues);
				Undefine(StrStringValue);
				Undefine(StrEndStringValues);

				Undefine(StrStartColors);
				Undefine(StrColor);
				Undefine(StrEndColors);

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

			bitmapSize = bmp.Height * bmp.Stride * bmp.BytesPerPixel;

			if (Writer != null)
			{
				if (string.IsNullOrEmpty(bmp.Type))
				{
					Writer.WriteLine();
					DefineHead(StrStartScreen, bmp.Name);
					DefineValue(StrImageAddress, Offset);
					DefineValue(StrImageSize, bitmapSize);
					DefineValue(StrImageWidth, bmp.Width);
					DefineValue(StrImageHeight, bmp.Height);
					DefineValue(StrImageStride, bmp.Stride);
					DefineValue(StrImageDepth, bmp.ColorDepth);
					DefineHead(StrEndScreen, bmp.Name);
				}
				else
				{
					// Save up all bitmaps with type until the end
					if (!Types.ContainsKey(bmp.Type))
						Types.Add(bmp.Type, new List<BitmapInfo>());

					Types[bmp.Type].Add(new BitmapInfo(bmp, Offset));
				}

				Writer.WriteLine();
				DefineHead(StrStartHotspots, bmp.Name);
				foreach (HotSpot spot in bmp.HotSpots)
				{
					Writer.WriteLine("\t" + StrHotspot + "({0}, {1}, {2}, {3}, {4}, {5})",
						spot.Name, spot.Group, spot.MinX, spot.MinY, spot.MaxX, spot.MaxY);
					if (string.IsNullOrEmpty(spot.Group))
						spot.Group = "";    // make sure not null

					if (!Groups.ContainsKey(spot.Group))
						Groups.Add(spot.Group, new List<HotSpot>());

					Groups[spot.Group].Add(spot);
				}
				DefineHead(StrEndHotspots, bmp.Name);

				Writer.WriteLine();
				DefineNamedValue(StrCountHotSpots, bmp.Name, bmp.HotSpots.Count);

				if (bmp.Locations.Count != 0)
				{
					Writer.WriteLine();
					DefineHead(StrStartLocations, bmp.Name);
					foreach (Location loc in bmp.Locations)
						Writer.WriteLine("\t" + StrLocation + "({0}, {1}, {2})", loc.Name, loc.X, loc.Y);
					DefineHead(StrEndLocations, bmp.Name);
				}

				if (bmp.Areas.Count != 0)
				{
					Writer.WriteLine();
					DefineHead(StrStartAreas, bmp.Name);
					foreach (Area area in bmp.Areas)
						Writer.WriteLine("\t" + StrArea + "({0}, {1}, {2}, {3}, {4})", area.Name, area.X, area.Y, area.Width, area.Height);
					DefineHead(StrEndAreas, bmp.Name);
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

		public void WriteColors(List<KeyValuePair<string, int>> Colors)
		{
			if (Colors.Count > 0)
			{
				Writer.WriteLine();
				DefineHead(StrStartColors, "");
				foreach (var color in Colors)
					DefineNamedValue("\t" + StrColor, color.Key, "0x" + color.Value.ToString("X6"));
				DefineHead(StrEndColors, "");
			}
		}

		public void WriteValues(List<ShowValue> Values)
		{
			if (Values.Count > 0)
			{
				// First do numeric values
				Writer.WriteLine();
				DefineHead(StrStartValues, "");
				foreach (ShowValue val in Values)
				{
					if (val.Value is string)
						continue;
					DefineNamedValue("\t" + StrValue, val.Name, val.Value);
				}
				DefineHead(StrEndValues, "");

				// Now do string values
				Writer.WriteLine();
				DefineHead(StrStartStringValues, "");
				foreach (ShowValue val in Values)
				{
					if (val.Value is string)
						DefineNamedValue("\t" + StrStringValue, val.Name, val.Value);
				}
				DefineHead(StrEndStringValues, "");
			}
		}

		byte[] ConvertColorBitmap(NamedBitmap bmp)
		{
			byte[] arPx, arPx32;
			int size;
			int pixel;

			size = bmp.Height * bmp.Stride;
			arPx = new byte[size * bmp.BytesPerPixel];
			arPx32 = new byte[size * 4];
			bmp.Bitmap.CopyPixels(arPx32, bmp.Stride * 4, 0);

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

		void Predefine0(string macro)
		{
			Writer.WriteLine(StrMacroPredfine, macro, "");
		}

		void Predefine1(string macro)
		{
			Writer.WriteLine(StrMacroPredfine, macro, "a");
		}

		void Predefine2(string macro)
		{
			Writer.WriteLine(StrMacroPredfine, macro, "a,b");
		}

		void Predefine3(string macro)
		{
			Writer.WriteLine(StrMacroPredfine, macro, "a,b,c");
		}

		void Predefine5(string macro)
		{
			Writer.WriteLine(StrMacroPredfine, macro, "a,b,c,d,e");
		}

		void Predefine6(string macro)
		{
			Writer.WriteLine(StrMacroPredfine, macro, "a,b,c,d,e,f");
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
