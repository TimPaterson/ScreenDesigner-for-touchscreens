# ScreenDesigner: Create screen images for touchscreens
ScreenDesigner is a Windows app for creating image bitmaps of any size for
use in embedded projects with an LCD screen. Here is a screenshot of 
a contrived example (which can be found in the Examples/Keypad folder):

![Example](https://raw.githubusercontent.com/TimPaterson/ScreenDesigner-for-touchscreens/master/Examples/Keypad/KeypadScreenshot.png)

The images are defined in XML using the editor of your choice. Whenever
you save the file, ScreenDesigner instantly updates the images. There is
no graphical interface that lets you position things by dragging them around.
But it does allow you to assign variables and create expressions that 
control the layout. Here is the first XML element of this example:

      <Set>
        ScreenHeight = 320;
        ScreenWidth = 200;
  	    BtnHeight = 60;
  	    BtnWidth = 60;
  	    KeySpace = 10;
  	    BtnFontSize = BtnHeight * 3 / 4;
        KeypadHeight = BtnHeight * 4 + KeySpace * 3;
        KeypadTop = ScreenHeight - KeypadHeight;
        DisplayMargin = 5;
        DisplayWidth = ScreenWidth - DisplayMargin * 2;
        DisplayHeight = KeypadTop - DisplayMargin * 2;
      </Set>

These are all user-created variable that are used later to set attributes of
elements like \<Rectangle\>, \<TextBlock\>, and \<Grid\>.

ScreenDesigner produces two output files:
- A binary file (.bin) with all images, uncompressed and undelineated.
- A header file (.h) with macros describing the images.

Here is a portion of the generated header file for this example:

	START_COLORS()
		DEFINE_COLOR(DisplayBackcolor, 0xFFFF00)
	END_COLORS()

	START_SCREEN(KeypadUp)
		IMAGE_ADDRESS(0)
		IMAGE_SIZE(128000)
		IMAGE_WIDTH(200)
		IMAGE_HEIGHT(320)
		IMAGE_STRIDE(200)
		IMAGE_DEPTH(Color16bpp)
	END_SCREEN(KeypadUp)

	START_HOTSPOTS(KeypadUp)
		DEFINE_HOTSPOT(Key_7, Digit, 0, 0, 59, 59)
		DEFINE_HOTSPOT(Key_8, Digit, 70, 0, 129, 59)
		DEFINE_HOTSPOT(Key_9, Digit, 140, 0, 199, 59)
		DEFINE_HOTSPOT(Key_4, Digit, 0, 70, 59, 129)
		DEFINE_HOTSPOT(Key_5, Digit, 70, 70, 129, 129)
		DEFINE_HOTSPOT(Key_6, Digit, 140, 70, 199, 129)
		DEFINE_HOTSPOT(Key_1, Digit, 0, 140, 59, 199)
		DEFINE_HOTSPOT(Key_2, Digit, 70, 140, 129, 199)
		DEFINE_HOTSPOT(Key_3, Digit, 140, 140, 199, 199)
		DEFINE_HOTSPOT(Key_sign, Digit, 0, 210, 59, 269)
		DEFINE_HOTSPOT(Key_0, Digit, 70, 210, 129, 269)
		DEFINE_HOTSPOT(Key_decimal, Digit, 140, 210, 199, 269)
	END_HOTSPOTS(KeypadUp)

	HOTSPOT_COUNT(KeypadUp, 12)

	START_AREAS(KeypadUp)
		DEFINE_AREA(Display, 5, 275, 190, 40)
	END_AREAS(KeypadUp)

You define macros in the code for your embedded application to use these
data as you see fit. You can include the header file multiple times with
different macro definitions as needed.

### More Information
Hopefully more documentation will be added over time. In the meantime, you
can review the examples. Also, scanning the XML schema ScreenDesigner.xsd
can help reveal all the elements, attributes, and enumeration values.
