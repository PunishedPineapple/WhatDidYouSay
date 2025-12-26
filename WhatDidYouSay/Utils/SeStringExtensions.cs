using System;

using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;

namespace WhatDidYouSay;

internal static class SeStringExtensions
{
	extension( SeString str )
	{
		public SeString Duplicate()
		{
			if( str == null ) return SeString.Empty;
			return SeString.Parse( str.Encode() ) ?? SeString.Empty;
		}

		public SeString RemoveLineBreaks( bool onlyNewlinePayloads = false, bool replaceNBSP = true )
		{
			str = str.Duplicate();
			str.RemoveLineBreaksInPlace( onlyNewlinePayloads, replaceNBSP );
			return str;
		}

		//***** TODO: See how SeHyphens are used and whether we need to handle them.
		public void RemoveLineBreaksInPlace( bool onlyNewlinePayloads = false, bool replaceNBSP = true )
		{
			for( int i = str.Payloads.Count - 1; i >= 0; --i )
			{
				if( str.Payloads[i] is TextPayload textPayload )
				{
					if( !onlyNewlinePayloads ) textPayload.Text = textPayload.Text?.ReplaceLineEndings( " " );
					if( replaceNBSP ) textPayload.Text = textPayload.Text?.Replace( '\xA0', ' ' );
					textPayload.Text = textPayload.Text?.Replace( ".  ", ". " );	//	The game appears to use single spaces after stops, but spaces before line breaks are inconsistent.
				}
				else if( str.Payloads[i] is NewLinePayload newLinePayload )
				{
					if( i > 0 )
					{
						int j = i - 1;
						while( j >= 0 && str.Payloads[j].Type != PayloadType.RawText ) --j;

						if( j >= 0 && str.Payloads[j] is TextPayload prevTextPayload )
						{
							//***** TODO: I have no idea how hyphenation across lines and stops are done in other languages.
							prevTextPayload.Text = prevTextPayload.Text.TrimEnd();
							if( prevTextPayload.Text.EndsWith( '-' ) ) prevTextPayload.Text = prevTextPayload.Text[..^1];
							else if( prevTextPayload.Text.EndsWith( '.' ) ) prevTextPayload.Text += ' ';
							else prevTextPayload.Text += ' ';
							str.Payloads.RemoveAt( i );
						}
					}
					else
					{
						str.Payloads.RemoveAt( i );
					}
				}
			}
		}

		public void ColorInPlace( UInt16 textColor, UInt16 glowColor, bool resetColorsAtEnd = true, bool terminateColorOnly = false )
		{
			if( !terminateColorOnly )
			{
				if( glowColor > 0 ) str.Payloads.Insert( 0, new UIGlowPayload( glowColor ) );
				if( textColor > 0 ) str.Payloads.Insert( 0, new UIForegroundPayload( textColor ) );
			}

			if( resetColorsAtEnd )
			{
				if( glowColor > 0 ) str.Payloads.Add( UIForegroundPayload.UIForegroundOff );
				if( textColor > 0 ) str.Payloads.Add( UIGlowPayload.UIGlowOff );
			}
		}

		//***** TODO: Probably remove.
		public SeString RemoveLineBreaks_Old()
		{
			SeString newStr = str.Duplicate();

			for( int i = newStr.Payloads.Count - 1; i >= 0; --i )
			{
				if( newStr.Payloads[i].Type == PayloadType.NewLine )
				{
					if( i > 0 )
					{
						int j = i - 1;
						while( j >= 0 && newStr.Payloads[j].Type != PayloadType.RawText ) --j;

						if( j >= 0 )
						{
							TextPayload prevTextPayload = (TextPayload)newStr.Payloads[j];
							if( prevTextPayload.Text.EndsWith( '-' ) )
							{
								newStr.Payloads[j] = new TextPayload( prevTextPayload.Text[..^1] );
								newStr.Payloads.RemoveAt( i );
							}
							else if( !prevTextPayload.Text.EndsWith( ' ' ) )
							{
								newStr.Payloads[i] = new TextPayload( " " );
							}
						}
					}
					else
					{
						newStr.Payloads.RemoveAt( i );
					}
				}
			}

			return newStr;
		}
	}
}
