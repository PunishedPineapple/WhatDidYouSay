using System;
using System.Numerics;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface;

namespace WhatDidYouSay;

internal static class ImGuiUtils
{
	public static UInt32 ColorVecToUInt( Vector4 color )
	{
		return
		(uint)( color.X * 255f ) << 0 |
		(uint)( color.Y * 255f ) << 8 |
		(uint)( color.Z * 255f ) << 16 |
		(uint)( color.W * 255f ) << 24;
	}

	public static Vector4 ColorUIntToVec( UInt32 color )
	{
		return new Vector4()
		{
			X = (float)( color & 0xFF ) / 255f,
			Y = (float)( color & 0xFF00 ) / 255f,
			Z = (float)( color & 0xFF0000 ) / 255f,
			W = (float)( color & 0xFF000000 ) / 255f
		};
	}

	public static void HelpMarker( string description, bool sameLine = true, string marker = "(?)" )
	{
		if( sameLine ) ImGui.SameLine();
		ImGui.TextDisabled( marker );
		TooltipLastItem( description );
	}

	public static void TooltipLastItem( string description )
	{
		if( ImGui.IsItemHovered() )
		{
			ImGui.BeginTooltip();
			ImGui.PushTextWrapPos( ImGui.GetFontSize() * 35.0f );
			ImGui.TextUnformatted( description );
			ImGui.PopTextWrapPos();
			ImGui.EndTooltip();
		}
	}

	public static void AddUnderline( Vector4 color, float thickness )
	{
		Vector2 min = ImGui.GetItemRectMin();
		Vector2 max = ImGui.GetItemRectMax();
		min.Y = max.Y;
		ImGui.GetWindowDrawList().AddLine( min, max, ColorVecToUInt( color ), thickness );
	}

	public static unsafe void CopyableAddress( string addressDescription, void* address ) => CopyableAddress( addressDescription, (nint)address );

	public static void CopyableAddress( string addressDescription, nint address )
	{
		ImGui.Text( $"{addressDescription} address:" );
		ImGui.SameLine();
		ImGui.Text( $"0x{address:X}" );
		if( ImGui.IsItemClicked() ) ImGui.SetClipboardText( $"{address:X}" );
		if( ImGui.IsItemHovered() )
		{
			AddUnderline( Vector4.One, 1f );
			ImGui.SetMouseCursor( ImGuiMouseCursor.Hand );
			ImGui.SameLine();
			ImGui.PushFont( UiBuilder.IconFont );
			try
			{
				ImGui.Text( "\uF0C5" );
			}
			finally
			{
				ImGui.PopFont();
			}
		}
	}

	public const float SectionSpacingSize = 20f;
}
