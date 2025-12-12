using System;

using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;

using FFXIVClientStructs.FFXIV.Client.Game.Object;

namespace WhatDidYouSay;

internal class NpcMessageInfo : IEquatable<NpcMessageInfo>
{
	public NpcMessageInfo()
	{
		TimeLastSeen = DateTimeOffset.UtcNow;
		HasBeenPrinted = false;
	}

	public NpcMessageInfo( GameObjectId speakerObjectID, ObjectKind speakerKind, UInt32 speakerBaseID, SeString speakerName, SeString message ) :
		this()
	{
		SpeakerObjectID = speakerObjectID;
		SpeakerKind = speakerKind;
		SpeakerBaseID = speakerBaseID;
		SpeakerName = speakerName;
		Message = message;
	}

	public GameObjectId SpeakerObjectID { get; init; } = 0xE0000000;
	public ObjectKind SpeakerKind { get; init; } = ObjectKind.None;
	public UInt32 SpeakerBaseID { get; init; } = 0;
	public SeString SpeakerName { get; init; } = SeString.Empty;
	public SeString Message { get; init; } = SeString.Empty;
	public DateTimeOffset TimeLastSeen { get; protected set; }
	public bool HasBeenPrinted { get; protected set; }

	public void FlagMessageSeenAgain() => TimeLastSeen = DateTimeOffset.UtcNow;
	public void FlagGameHandledPrinting() => HasBeenPrinted = true;

	public bool PrintToChatLog()
	{
		//	Rate limit this as a last resort in case we messed up.
		var currentTime = DateTimeOffset.UtcNow;
		if( currentTime - LastTimeChatPrinted >= Plugin.CurrentConfiguration.MinTimeBetweenChatPrints )
		{
			//	Make a copy so that we can do some formatting without poisoning the history.
			var msg = Message.Duplicate();

			//	Remove line breaks if desired.
			if( !Plugin.CurrentConfiguration.KeepLineBreaks )
			{
				msg.RemoveLineBreaksInPlace( false );
			}

			//	Get as reasonable a speaker name we can.
			var speakerName = GetBestAvailableSenderName();

			//***** TODO: Color speaker name.  This is probably not doable; the chat log does not like color payloads in the sender name, and we have no reasonable way to set the whole line payload in the log text.

			//***** TODO: Message coloring.
			//***** If this is configured for no color, grab the configured text color from the game for the channel being used.
			//***** Do it that way so that we overwrite the sender name color if that is set, but message color is not (and the colon gets colored correctly too).
			//***** Make sure that the color selector either has a zero option, or we do a checkbox on whether to color the text.
			//msg.ColorInPlace( mConfiguration.NpcMessageTextColor, mConfiguration.NpcMessageGlowColor );
			//speakerName.ColorInPlace( mConfiguration.SenderTextColor_Unknown, mConfiguration.SenderGlowColor_Unknown );

			//speakerName.ColorInPlace( Plugin.CurrentConfiguration.NpcMessageTextColor, Plugin.CurrentConfiguration.SenderGlowColor_Unknown, false, false );
			msg.ColorInPlace( Plugin.CurrentConfiguration.NpcMessageTextColor, Plugin.CurrentConfiguration.NpcMessageGlowColor, false, true );

			DalamudAPI.ChatGui.Print( new()
			{
				Type = Plugin.CurrentConfiguration.ChatChannelToUse,
				Name = speakerName,
				Message = msg,
			} );

			HasBeenPrinted = true;
			LastTimeChatPrinted = currentTime;

			return true;
		}
		else
		{
			return false;
		}
	}

	protected SeString GetBestAvailableSenderName()
	{
		if( !string.IsNullOrWhiteSpace( SpeakerName?.TextValue ) ) return SpeakerName;

		if( SpeakerKind == ObjectKind.BattleNpc )
		{
			//if( BNpcBase has a custom name that we made )
			{
				//***** TODO: Custom BNpc name.
			}
			//else if( BNpcBase has customize information for player races )
			{
				//***** TODO: Procedural Name Generation.
			}
		}
		else if( SpeakerKind == ObjectKind.EventNpc )
		{
			//if( ENpcBase has a custom name that we made )
			{
				//***** TODO: Custom ENpc name.
			}
			//else if( ENpcBase has customize information for player races )
			{
				//***** TODO: Procedural Name Generation.
			}
		}

		if( Plugin.CurrentConfiguration?.DefaultSenderName is string defaultSender &&
			Plugin.IsValidCustomSenderName( defaultSender ) )
		{
			return defaultSender;
		}

		return SeString.Empty;
	}

	public static DateTimeOffset LastTimeChatPrinted { get; protected set; } = DateTimeOffset.UnixEpoch;

	public bool Equals( NpcMessageInfo? rhs, bool ignoreSenderID = false, bool ignoreTimeLastSeen = true )
	{
		//***** TODO: It is conceivable that we might have messages that differ only in non-text payloads (i.e., a repeat with italics for emphasis).
		return	rhs is not null &&
				SpeakerName.TextValue.Equals( rhs.SpeakerName.TextValue ) &&
				Message.TextValue.Equals( rhs.Message.TextValue ) &&
				( ignoreSenderID || SpeakerObjectID == rhs.SpeakerObjectID ) &&
				( ignoreTimeLastSeen || TimeLastSeen == rhs.TimeLastSeen );
	}

	public static bool operator ==( NpcMessageInfo? lhs, NpcMessageInfo? rhs ) => lhs is null ? rhs is null : lhs.Equals( rhs );
	public static bool operator !=( NpcMessageInfo? lhs, NpcMessageInfo? rhs ) => !( lhs == rhs );
	public bool Equals( NpcMessageInfo? rhs ) => this.Equals( rhs, false, true );
	public override bool Equals( object? obj ) => obj is NpcMessageInfo rhs && this.Equals( rhs );
	public override int GetHashCode() => HashCode.Combine( SpeakerObjectID.GetHashCode(), Message?.GetHashCode() );
}
