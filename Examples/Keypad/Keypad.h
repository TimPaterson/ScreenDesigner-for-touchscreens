#ifndef START_SCREEN
#define START_SCREEN(a)
#endif
#ifndef IMAGE_ADDRESS
#define IMAGE_ADDRESS(a)
#endif
#ifndef IMAGE_SIZE
#define IMAGE_SIZE(a)
#endif
#ifndef IMAGE_WIDTH
#define IMAGE_WIDTH(a)
#endif
#ifndef IMAGE_HEIGHT
#define IMAGE_HEIGHT(a)
#endif
#ifndef IMAGE_STRIDE
#define IMAGE_STRIDE(a)
#endif
#ifndef IMAGE_DEPTH
#define IMAGE_DEPTH(a)
#endif
#ifndef END_SCREEN
#define END_SCREEN(a)
#endif
#ifndef START_HOTSPOTS
#define START_HOTSPOTS(a)
#endif
#ifndef DEFINE_HOTSPOT
#define DEFINE_HOTSPOT(a,b,c,d,e,f)
#endif
#ifndef END_HOTSPOTS
#define END_HOTSPOTS(a)
#endif
#ifndef HOTSPOT_COUNT
#define HOTSPOT_COUNT(a,b)
#endif
#ifndef START_LOCATIONS
#define START_LOCATIONS(a)
#endif
#ifndef DEFINE_LOCATION
#define DEFINE_LOCATION(a,b,c)
#endif
#ifndef END_LOCATIONS
#define END_LOCATIONS(a)
#endif
#ifndef START_AREAS
#define START_AREAS(a)
#endif
#ifndef DEFINE_AREA
#define DEFINE_AREA(a,b,c,d,e)
#endif
#ifndef END_AREAS
#define END_AREAS(a)
#endif
#ifndef START_GROUP
#define START_GROUP(a)
#endif
#ifndef GROUP_HOTSPOT
#define GROUP_HOTSPOT(a,b,c,d,e,f)
#endif
#ifndef END_GROUP
#define END_GROUP(a)
#endif
#ifndef START_VALUES
#define START_VALUES()
#endif
#ifndef DEFINE_VALUE
#define DEFINE_VALUE(a,b)
#endif
#ifndef END_VALUES
#define END_VALUES()
#endif
#ifndef START_STR_VALUES
#define START_STR_VALUES()
#endif
#ifndef DEFINE_STR_VALUE
#define DEFINE_STR_VALUE(a,b)
#endif
#ifndef END_STR_VALUES
#define END_STR_VALUES()
#endif
#ifndef START_COLORS
#define START_COLORS()
#endif
#ifndef DEFINE_COLOR
#define DEFINE_COLOR(a,b)
#endif
#ifndef END_COLORS
#define END_COLORS()
#endif
#ifndef SCREEN_FILE_LENGTH
#define SCREEN_FILE_LENGTH(a)
#endif

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

START_SCREEN(KeypadDown)
	IMAGE_ADDRESS(128000)
	IMAGE_SIZE(128000)
	IMAGE_WIDTH(200)
	IMAGE_HEIGHT(320)
	IMAGE_STRIDE(200)
	IMAGE_DEPTH(Color16bpp)
END_SCREEN(KeypadDown)

START_HOTSPOTS(KeypadDown)
	DEFINE_HOTSPOT(Key_7, Digit, 0, 50, 59, 109)
	DEFINE_HOTSPOT(Key_8, Digit, 70, 50, 129, 109)
	DEFINE_HOTSPOT(Key_9, Digit, 140, 50, 199, 109)
	DEFINE_HOTSPOT(Key_4, Digit, 0, 120, 59, 179)
	DEFINE_HOTSPOT(Key_5, Digit, 70, 120, 129, 179)
	DEFINE_HOTSPOT(Key_6, Digit, 140, 120, 199, 179)
	DEFINE_HOTSPOT(Key_1, Digit, 0, 190, 59, 249)
	DEFINE_HOTSPOT(Key_2, Digit, 70, 190, 129, 249)
	DEFINE_HOTSPOT(Key_3, Digit, 140, 190, 199, 249)
	DEFINE_HOTSPOT(Key_sign, Digit, 0, 260, 59, 319)
	DEFINE_HOTSPOT(Key_0, Digit, 70, 260, 129, 319)
	DEFINE_HOTSPOT(Key_decimal, Digit, 140, 260, 199, 319)
END_HOTSPOTS(KeypadDown)

HOTSPOT_COUNT(KeypadDown, 12)

START_AREAS(KeypadDown)
	DEFINE_AREA(Display, 5, 5, 190, 40)
END_AREAS(KeypadDown)

SCREEN_FILE_LENGTH(256000)

#undef START_SCREEN
#undef IMAGE_ADDRESS
#undef IMAGE_SIZE
#undef IMAGE_WIDTH
#undef IMAGE_HEIGHT
#undef IMAGE_STRIDE
#undef IMAGE_DEPTH
#undef END_SCREEN
#undef START_HOTSPOTS
#undef DEFINE_HOTSPOT
#undef END_HOTSPOTS
#undef HOTSPOT_COUNT
#undef START_LOCATIONS
#undef DEFINE_LOCATION
#undef END_LOCATIONS
#undef START_AREAS
#undef DEFINE_AREA
#undef END_AREAS
#undef START_GROUP
#undef GROUP_HOTSPOT
#undef END_GROUP
#undef START_VALUES
#undef DEFINE_VALUE
#undef END_VALUES
#undef START_STR_VALUES
#undef DEFINE_STR_VALUE
#undef END_STR_VALUES
#undef START_COLORS
#undef DEFINE_COLOR
#undef END_COLORS
#undef SCREEN_FILE_LENGTH
