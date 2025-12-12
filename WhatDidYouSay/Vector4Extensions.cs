using System;
using System.Numerics;

namespace WhatDidYouSay;

internal static class Vector4Extensions
{
	public static UInt32 AsImGuiColor( this Vector4 color )
	{
		return
		(UInt32)( color.X * 255f ) << 0 |
		(UInt32)( color.Y * 255f ) << 8 |
		(UInt32)( color.Z * 255f ) << 16 |
		(UInt32)( color.W * 255f ) << 24;
	}
}
