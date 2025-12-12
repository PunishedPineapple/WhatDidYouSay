using System;
using System.Buffers.Binary;
using System.Numerics;

namespace WhatDidYouSay;

internal static class UInt32Extensions
{
	public static Vector4 ToColorVec_ImGui( this UInt32 color )
	{
		return new Vector4()
		{
			X = ( color & 0x000000FF ) / 255f,
			Y = ( color & 0x0000FF00 ) / 255f,
			Z = ( color & 0x00FF0000 ) / 255f,
			W = ( color & 0xFF000000 ) / 255f,
		};
	}

	public static Vector4 ToColorVec_SE( this UInt32 color )
	{
		return new Vector4()
		{
			X = ( color & 0xFF000000 ) / 255f,
			Y = ( color & 0x00FF0000 ) / 255f,
			Z = ( color & 0x0000FF00 ) / 255f,
			W = ( color & 0x000000FF ) / 255f,
		};
	}

	public static UInt32 ByteSwap( this UInt32 color ) => BinaryPrimitives.ReverseEndianness( color );
}
