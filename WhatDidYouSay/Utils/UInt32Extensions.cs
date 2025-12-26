using System;
using System.Buffers.Binary;
using System.Numerics;

namespace WhatDidYouSay;

internal static class UInt32Extensions
{
	extension( UInt32 color )
	{
		public Vector4 ToColorVec_ImGui()
		{
			return new Vector4()
			{
				R = ( color & 0x000000FF ) / 255f,
				G = ( color & 0x0000FF00 ) / 255f,
				B = ( color & 0x00FF0000 ) / 255f,
				A = ( color & 0xFF000000 ) / 255f,
			};
		}

		public Vector4 ToColorVec_SE()
		{
			return new Vector4()
			{
				R = ( color & 0xFF000000 ) / 255f,
				G = ( color & 0x00FF0000 ) / 255f,
				B = ( color & 0x0000FF00 ) / 255f,
				A = ( color & 0x000000FF ) / 255f,
			};
		}

		public UInt32 ByteSwap() => BinaryPrimitives.ReverseEndianness( color );
	}
}
