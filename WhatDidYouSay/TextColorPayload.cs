using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Dalamud.Game.Text.SeStringHandling;

namespace WhatDidYouSay;

/// <summary>
/// An SeString Payload that allows text to have a specific color. The color selected will be determined by the
/// <see cref="Lumina.Excel.Sheets.UIColor.Dark"/> theme's coloring, regardless of the active theme.
/// </summary>
internal class TextColorPayload : Payload
{
	private UInt32 mColor;

	public TextColorPayload( UInt32 color )
	{
		mColor = color;
	}

	public override PayloadType Type => PayloadType.Unknown;

	public override string ToString()
	{
		return $"TextColorPayload - Color: 0x{mColor:X}";
	}

	protected override byte[] EncodeImpl()
	{
		bool isTerminator = mColor == 0;
		var colorBytes = BitConverter.GetBytes( mColor );
		var chunkLen = isTerminator ? 2 : ( colorBytes.Length + 1 );

		byte valueCode = isTerminator ? (byte)0xEC : (byte)0xFE;
		var bytes = new List<byte>( new byte[]
		{
			START_BYTE, 0x13, (byte)chunkLen, valueCode,
		} );

		if( !isTerminator ) bytes.AddRange( colorBytes );
		else bytes.Add( 0xEC );
		
		bytes.Add( END_BYTE );

		return bytes.ToArray();
	}

	protected override void DecodeImpl( BinaryReader reader, long endOfStream )
	{
		byte colorType = reader.ReadByte();
		byte[] colorBytes  = colorType switch
		{
			0xEC => [0],
			0xF6 => reader.ReadBytes( 3 ),
			0xFE => reader.ReadBytes( 4 ),
			_ => [0],
		};

		if( colorBytes.Length == 3 )
		{
			var byteList = colorBytes.ToList();
			byteList.Add( 0 );
			colorBytes = byteList.ToArray();
		}

		if( colorBytes.Length == 4 )
		{
			mColor = BitConverter.ToUInt32( colorBytes );
		}
		else
		{
			DalamudAPI.PluginLog.Debug( "Unexpected length in color payload." );
			mColor = 0;
		}

	}
}
