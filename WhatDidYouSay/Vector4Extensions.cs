using System;
using System.Numerics;

namespace WhatDidYouSay;

internal static class Vector4Extensions
{
	extension( Vector4 color )
	{
		public float R { get => color.X; set => color.X = value; }
		public float G { get => color.Y; set => color.Y = value; }
		public float B { get => color.Z; set => color.Z = value; }
		public float A { get => color.W; set => color.W = value; }

		public UInt32 AsImGuiColor()
		{
			return
			(UInt32)( color.R * 255f ) << 0 |
			(UInt32)( color.G * 255f ) << 8 |
			(UInt32)( color.B * 255f ) << 16 |
			(UInt32)( color.A * 255f ) << 24;
		}

		public UInt32 AsSEColor()
		{
			return
			(UInt32)( color.R * 255f ) << 24 |
			(UInt32)( color.G * 255f ) << 16 |
			(UInt32)( color.B * 255f ) << 8 |
			(UInt32)( color.A * 255f ) << 0;
		}
	}
}
