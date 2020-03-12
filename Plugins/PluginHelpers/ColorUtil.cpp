// PluginHelpers.cpp : Defines the exported functions for the DLL application.
//

#include "stdafx.h"
#include "ColorUtil.h"

#include <math.h>

#include <Shlwapi.h>
#pragma comment(lib, "shlwapi.lib")

////////////////////////////////////////////////////////////////////////////////////////////////

using namespace System;

using namespace Abstractspoon::Tdl::PluginHelpers;

////////////////////////////////////////////////////////////////////////////////////////////////

Windows::Media::Color ColorUtil::MediaColor::AdjustLighting(Windows::Media::Color color, float amount, bool rgbMethod)
{
	COLORREF rgbIn = RGB(color.R, color.G, color.B);
	COLORREF rgbOut = (COLORREF)ColorUtil::AdjustLighting(rgbIn, amount, rgbMethod);

	return Windows::Media::Color::FromArgb(color.A, (int)GetRValue(rgbOut), (int)GetGValue(rgbOut), (int)GetBValue(rgbOut));
}

Windows::Media::Color ColorUtil::MediaColor::SetLuminance(Windows::Media::Color color, float luminance)
{
	COLORREF rgbIn = RGB(color.R, color.G, color.B);
	COLORREF rgbOut = (COLORREF)ColorUtil::SetLuminance(rgbIn, luminance);

	return Windows::Media::Color::FromArgb(color.A, (int)GetRValue(rgbOut), (int)GetGValue(rgbOut), (int)GetBValue(rgbOut));
}

Windows::Media::Color ColorUtil::MediaColor::GetColor(UInt32 rgbColor)
{
	return GetColor(rgbColor, 255);
}

Windows::Media::Color ColorUtil::MediaColor::GetColor(UInt32 rgbColor, unsigned char opacity)
{
	return System::Windows::Media::Color::FromArgb(opacity, (Byte)GetRValue(rgbColor), (Byte)GetGValue(rgbColor), (Byte)GetBValue(rgbColor));
}

Windows::Media::Color ColorUtil::MediaColor::GetBestTextColor(Windows::Media::Color backColor)
{
	if (GetLuminance(backColor) > 0.5)
		return Windows::Media::Colors::Black;

	// else
	return Windows::Media::Colors::White;
}

float ColorUtil::MediaColor::GetLuminance(Windows::Media::Color color)
{
	COLORREF rgbIn = RGB(color.R, color.G, color.B);

	return ColorUtil::GetLuminance(rgbIn);
}

////////////////////////////////////////////////////////////////////////////////////////////////

Drawing::Color ColorUtil::DrawingColor::AdjustLighting(Drawing::Color color, float amount, bool rgbMethod)
{
	COLORREF rgbIn = RGB(color.R, color.G, color.B);
	COLORREF rgbOut = ColorUtil::AdjustLighting(rgbIn, amount, rgbMethod);

	return Drawing::Color::FromArgb(color.A, (int)GetRValue(rgbOut), (int)GetGValue(rgbOut), (int)GetBValue(rgbOut));
}

Drawing::Color ColorUtil::DrawingColor::SetLuminance(Drawing::Color color, float luminance)
{
	COLORREF rgbIn = RGB(color.R, color.G, color.B);
	COLORREF rgbOut = ColorUtil::SetLuminance(rgbIn, luminance);

	return Drawing::Color::FromArgb(color.A, (int)GetRValue(rgbOut), (int)GetGValue(rgbOut), (int)GetBValue(rgbOut));
}

Drawing::Color ColorUtil::DrawingColor::GetColor(UInt32 rgbColor)
{
	return GetColor(rgbColor, 255);
}

Drawing::Color ColorUtil::DrawingColor::GetColor(UInt32 rgbColor, unsigned char opacity)
{
	return System::Drawing::Color::FromArgb(opacity, (Byte)GetRValue(rgbColor), (Byte)GetGValue(rgbColor), (Byte)GetBValue(rgbColor));
}

Drawing::Color ColorUtil::DrawingColor::GetBestTextColor(Drawing::Color backColor)
{
	if (GetLuminance(backColor) > 0.5)
		return Drawing::Color::Black;

	// else
	return Drawing::Color::White;
}

float ColorUtil::DrawingColor::GetLuminance(Drawing::Color color)
{
	COLORREF rgbIn = RGB(color.R, color.G, color.B);

	return ColorUtil::GetLuminance(rgbIn);
}

bool ColorUtil::DrawingColor::IsTransparent(Drawing::Color color, bool whiteIsTransparent)
{
	if (color.ToArgb() == Drawing::Color::Transparent.ToArgb())
		return true;

	if (whiteIsTransparent && (color.ToArgb() == Drawing::Color::White.ToArgb()))
		return true;

	return false;
}

String^ ColorUtil::DrawingColor::ToHtml(Drawing::Color color, bool whiteIsTransparent)
{
	if (IsTransparent(color, whiteIsTransparent))
		return String::Empty;

	return Drawing::ColorTranslator::ToHtml(Drawing::Color::FromArgb(color.ToArgb()));
}

Drawing::Color ColorUtil::DrawingColor::FromHtml(String^ color)
{
	if (String::IsNullOrEmpty(color))
		return Drawing::Color::Transparent;

	return Drawing::ColorTranslator::FromHtml(color);
}

bool ColorUtil::DrawingColor::Equals(Drawing::Color color1, Drawing::Color color2)
{
	return (color1.ToArgb() == color2.ToArgb());
}

Drawing::Color ColorUtil::DrawingColor::Copy(Drawing::Color color)
{
	return Drawing::Color::FromArgb(color.ToArgb());
}

////////////////////////////////////////////////////////////////////////////////////////////////

float ColorUtil::GetLuminance(UInt32 rgbColor)
{
	return (((GetBValue(rgbColor) * 0.1f) + (GetGValue(rgbColor) * 0.6f) + (GetRValue(rgbColor) * 0.3f)) / 255.0f);
}

UInt32 ColorUtil::SetLuminance(UInt32 rgbColor, float luminance)
{
	luminance = max(0.0f, min(1.0f, luminance));

	WORD wHue = 0, wLuminance = 0, wSaturation = 0;
	ColorRGBToHLS((COLORREF)rgbColor, &wHue, &wLuminance, &wSaturation);

	return ColorHLSToRGB(wHue, (WORD)(luminance * 240), wSaturation);
}

UInt32 ColorUtil::AdjustLighting(UInt32 rgbColor, float amount, bool rgbMethod)
{
	if (rgbMethod)
	{
		BYTE btRed = GetRValue(rgbColor);
		BYTE btGreen = GetGValue(rgbColor);
		BYTE btBlue = GetBValue(rgbColor);
		
		if (amount > 0.0) // Lighter
		{
			btRed = (BYTE)min(255, (btRed + ((255 - btRed) * amount)));
			btGreen = (BYTE)min(255, (btGreen + ((255 - btGreen) * amount)));
			btBlue = (BYTE)min(255, (btBlue + ((255 - btBlue) * amount)));
		}
		else // < 0.0 - Darker
		{
			btRed = (BYTE)max(0, (btRed + (btRed * amount)));
			btGreen = (BYTE)max(0, (btGreen + (btGreen * amount)));
			btBlue = (BYTE)max(0, (btBlue + (btBlue * amount)));
		}

		return RGB(btRed, btGreen, btBlue);
	}

	// else
	return ColorUtil::SetLuminance(rgbColor, (ColorUtil::GetLuminance(rgbColor) + amount));
}
