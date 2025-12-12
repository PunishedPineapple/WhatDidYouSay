using System;
using System.Buffers.Binary;
using System.Linq;

using CheapLoc;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface;

using Lumina.Excel.Sheets;
using Lumina.Extensions;

namespace WhatDidYouSay;

internal class UITextColorSelector : IDisposable
{
	public UITextColorSelector( UIColor[] availableColors, string identifierPrefix )
	{
		mUIColors = availableColors ?? throw new Exception( "Provided UI color array must not be null." );

		mUIIdentifierPrefix = identifierPrefix;
		if( string.IsNullOrEmpty( mUIIdentifierPrefix ) )
		{
			throw new Exception( "A valid prefix for ImGui identifiers must be provided." );
		}
	}

	protected UITextColorSelector() {}

	public void Dispose()
	{

	}

	public void Draw( ref ushort currentTextColor, ref ushort currentGlowColor, bool sameLine = true, bool showOptionsForNone = false )
	{
		if( sameLine ) ImGui.SameLine();

		ImGui.PushID( mUIIdentifierPrefix );

		try
		{
			//	The new ImGui stuff is missing ArrowButton, so we have to make our own for now.
			try
			{
				ImGui.PushFont( UiBuilder.IconFont );
				string iconStr = mShowColorSelectors ? "\uF078" : "\uF054";
				//if( ImGui.ArrowButton( "CraftingMaterialTextColorButton", mShowColorSelectors ? ImGuiDir.Down : ImGuiDir.Right ) )
				if( ImGui.Button( iconStr ) )
				{
					mShowColorSelectors = !mShowColorSelectors;
				}
			}
			finally
			{
				ImGui.PopFont();
			}

			ImGuiUtils.TooltipLastItem( Loc.Localize( "Help: Settings - Color Dropdown", "Click here to show/hide the text colors." ) );

			if( mShowColorSelectors )
			{
				EnsureValidColorID( ref currentTextColor );
				ImGui.Text( Loc.Localize( "Settings: Label - Text Color", "Text:" ) );
				ImGui.SameLine();
				DrawUIColorDropdown( ref currentTextColor, "", "UIColorSelectorTextColor", showOptionsForNone );

				EnsureValidColorID( ref currentGlowColor );
				ImGui.Text( Loc.Localize( "Settings: Label - Glow Color", "Glow:" ) );
				ImGui.SameLine();
				DrawUIColorDropdown( ref currentGlowColor, "", "UIColorSelectorGlowColor", showOptionsForNone );
			}
		}
		finally
		{
			ImGui.PopID();
		}
	}

	protected void DrawUIColorDropdown( ref ushort colorID, string dropdownTitle, string additionalDropdownID, bool showOptionForNone = false )
	{
		if( mUIColors?.Length > 0 )
		{
			var defaultTextColor = ImGui.GetColorU32( ImGuiCol.Text );
			var selectedItemDrawColor = GetDrawColorForColorID( colorID, mCurrentUITheme );
			ImGui.PushStyleColor( ImGuiCol.Text, selectedItemDrawColor );

			try
			{
				//***** TODO: Make "none" show correctly when the dropdown is closed.
				string comboText =	colorID > 0 ?
									string.Format( Loc.Localize( "Settings: Dropdown - Highlight Text Color", "UI Color {0}" ), colorID ) :
									Loc.Localize( "Settings: Dropdown - UI Text Color: None", "None" );

				if( ImGui.BeginCombo( dropdownTitle + "###UIColorDropdown_" + additionalDropdownID, comboText ) )
				{
					try
					{
						if( showOptionForNone )
						{
							try
							{
								ImGui.PushStyleColor( ImGuiCol.Text, defaultTextColor );
								if( ImGui.Selectable( Loc.Localize( "Settings: Dropdown - UI Text Color: None", "None" ) ) ) colorID = 0;
								if( colorID == 0 ) ImGui.SetItemDefaultFocus();
							}
							finally
							{
								ImGui.PopStyleColor();
							}
						}

						foreach( var color in mUIColors )
						{
							if( color.RowId == 0 ) continue;

							var colorTextString = string.Format( Loc.Localize( "Settings: Dropdown - UI Text Color", "UI Color {0}" ), color.RowId );

							try
							{
								ImGui.PushStyleColor( ImGuiCol.Text, GetDrawColorForUIColor( color, mCurrentUITheme ) );
								if( ImGui.Selectable( colorTextString ) ) colorID = (ushort)color.RowId;
								if( color.RowId == colorID ) ImGui.SetItemDefaultFocus();
							}
							finally
							{
								ImGui.PopStyleColor();
							}
						}
					}
					finally
					{
						ImGui.EndCombo();
					}
				}
			}
			finally
			{
				ImGui.PopStyleColor();
			}
		}
	}

	protected void EnsureValidColorID( ref ushort colorID )
	{
		var localColorIDCopy = colorID;
		if( mUIColors?.Any( x => x.RowId == localColorIDCopy ) != true ) colorID = 0;
	}

	protected uint GetDrawColorForColorID( ushort colorID, byte theme )
	{
		if( mUIColors.TryGetFirst( x => x.RowId == colorID && x.RowId != 0, out UIColor color ) )
		{
			return GetDrawColorForUIColor( color, theme );
		}
		else
		{
			return ImGui.GetColorU32( ImGuiCol.Text );
		}
	}

	protected uint GetDrawColorForUIColor( UIColor color, byte theme )
	{
		UInt32 retVal;

		retVal = theme switch
		{
			1 => BinaryPrimitives.ReverseEndianness( color.Light ),
			2 => BinaryPrimitives.ReverseEndianness( color.ClassicFF ),
			3 => BinaryPrimitives.ReverseEndianness( color.ClearBlue ),
			_ => BinaryPrimitives.ReverseEndianness( color.Dark ),
		};

		//	Low-limit the alpha so that the user can always see something.
		return retVal | 0x40_00_00_00;
	}

	//	We want to cache this on login so that the colors are always correct for the currently-
	//	displayed theme (since a configuration change may not yet have taken effect).
	public static void CacheUITheme()
	{
		if( DalamudAPI.ClientState.IsLoggedIn )
		{
			mCurrentUITheme = GetConfiguredUITheme();
		}
		else
		{
			//	The game always uses the default theme when not logged in to a character.
			mCurrentUITheme = 0;
		}
	}

	internal static byte GetConfiguredUITheme()
	{
		if( DalamudAPI.GameConfig.TryGet( Dalamud.Game.Config.SystemConfigOption.ColorThemeType, out uint theme ) )
		{
			return (byte)theme;
		}
		else
		{
			return 0;
		}
	}

	private static byte mCurrentUITheme = 0;

	private readonly UIColor[] mUIColors = null;
	private bool mShowColorSelectors = false;
	private readonly string mUIIdentifierPrefix;
}
