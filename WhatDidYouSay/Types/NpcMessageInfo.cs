using System;

using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;

using FFXIVClientStructs.FFXIV.Client.Game.Object;

using ObjectKind = Dalamud.Game.ClientState.Objects.Enums.ObjectKind;

namespace WhatDidYouSay;

internal class NpcMessageInfo : IEquatable<NpcMessageInfo>
{
	public NpcMessageInfo()
	{
		TimeFirstSeen = DateTimeOffset.UtcNow;
		TimeLastSeen = TimeFirstSeen;
		HasBeenPrinted = false;
	}

	public NpcMessageInfo( GameObjectId speakerObjectID, ObjectKind speakerKind, UInt32 speakerBaseID, UInt32 speakerNameID, SeString speakerName, SeString message ) :
		this()
	{
		SpeakerObjectID = speakerObjectID;
		SpeakerKind = speakerKind;
		SpeakerBaseID = speakerBaseID;
		SpeakerNameID = speakerNameID;
		SpeakerName = speakerName;
		Message = message;
	}

	public GameObjectId SpeakerObjectID { get; init; } = 0xE0000000;
	public ObjectKind SpeakerKind { get; init; } = ObjectKind.None;
	public UInt32 SpeakerBaseID { get; init; } = 0;
	public UInt32 SpeakerNameID { get; init; } = 0;
	public SeString SpeakerName { get; init; } = SeString.Empty;
	public SeString Message { get; init; } = SeString.Empty;
	public DateTimeOffset TimeFirstSeen { get; protected set; }
	public DateTimeOffset TimeLastSeen { get; protected set; }
	public bool HasBeenPrinted { get; protected set; }
	public bool IsExpired( TimeSpan lifetime ) => DateTimeOffset.UtcNow - TimeLastSeen > lifetime;

	public void FlagMessageSeenAgain() => TimeLastSeen = DateTimeOffset.UtcNow;
	public void FlagGameHandledPrinting() => HasBeenPrinted = true;
	public bool PrintToChatLog()
	{
		//	Rate limit this as a last resort in case we messed up.
		var currentTime = DateTimeOffset.UtcNow;
		if( currentTime - LastTimeChatPrinted >= Plugin.CurrentConfiguration?.MinTimeBetweenChatPrints )
		{
			//	Make a copy so that we can do some formatting without poisoning the history.
			var msg = Message.Duplicate();

			//	Remove line breaks if desired.
			if( !Plugin.CurrentConfiguration.KeepLineBreaks )
			{
				msg.RemoveLineBreaksInPlace();
			}

			//	Get as reasonable a speaker name we can.
			var speakerName = GetBestAvailableSenderName();

			//	Append the object base ID if desired.
			if( Plugin.CurrentConfiguration?.ShowNPCIDInChatMessage == true )
			{
				string kindStr = SpeakerKind switch
				{
					ObjectKind.BattleNpc => "BNpc",
					ObjectKind.EventNpc => "ENpc",
					_ => "Unk",
				};

				msg.Payloads.Add( new UIForegroundPayload( 4 ) );
				msg.Payloads.Add( new TextPayload( $" ({kindStr} {SpeakerBaseID})" ) );
				msg.Payloads.Add( new UIForegroundPayload( 0 ) );
			}

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
		//	Game-given name always takes precedence.
		if( !string.IsNullOrWhiteSpace( SpeakerName?.TextValue ) )
		{
			return SpeakerName;
		}

		//	Custom and debug names.
		if( SpeakerKind == ObjectKind.BattleNpc )
		{
			if( Plugin.CurrentConfiguration?.UseCuratedNames == true )
			{
				//***** TODO: Custom BNpc name.
				return "TEMP CUSTOM BNPC";
			}
			else if( Plugin.CurrentConfiguration?.UseProceduralNames == true )
			{
				//***** TODO: Procedural Name Generation if BNpcBase has customize information for player races
				return "TEMP PROC BNPC";
			}
			else if( Plugin.CurrentConfiguration?.ShowNPCIDForUnnamedNPCs == true )
			{
				return $"BNpc {SpeakerBaseID}";
			}
		}
		else if( SpeakerKind == ObjectKind.EventNpc )
		{
			if( Plugin.CurrentConfiguration?.UseCuratedNames == true )
			{
				//***** TODO: Custom ENpc name.
				return "TEMP CUSTOM ENPC";
			}
			else if( Plugin.CurrentConfiguration?.UseProceduralNames == true )
			{
				//***** TODO: Procedural Name Generation if ENpcBase has customize information for player races
				return "TEMP PROC ENPC";
			}
			else if( Plugin.CurrentConfiguration?.ShowNPCIDForUnnamedNPCs == true )
			{
				return $"ENpc {SpeakerBaseID}";
			}
		}
		else if( Plugin.CurrentConfiguration?.ShowNPCIDForUnnamedNPCs == true )
		{
			return $"Unk 0x{SpeakerBaseID:X}";
		}

		//	Configured default name.
		if( Plugin.CurrentConfiguration?.DefaultSenderName is string defaultSender &&
			Plugin.IsValidCustomSenderName( defaultSender ) )
		{
			return defaultSender;
		}

		//	Hard fallback.
		return "NPC";
	}

	public static DateTimeOffset LastTimeChatPrinted { get; protected set; } = DateTimeOffset.UnixEpoch;

	internal string GetDebugString()
	{
		string str = $"Speaker ID: 0x{(ulong)SpeakerObjectID:X}, Speaker Base ID: {SpeakerBaseID}, Speaker Name ID: {SpeakerNameID}, Speaker Kind: {SpeakerKind}";
		str += $"\r\n[{TimeFirstSeen.ToLocalTime():HH:mm:ss}]{SpeakerName.TextValue}: {Message.TextValue}";
		str += $"\r\nLast Seen: {TimeLastSeen.ToLocalTime():HH:mm:ss} ";
		return str;
	}

	public bool Equals( NpcMessageInfo? rhs, bool ignoreTimeLastSeen = true, SenderComparisonType senderComparison = SenderComparisonType.ObjectID )
	{
		//***** TODO: It is conceivable that we might have messages that differ only in non-text payloads (i.e., a repeat with italics for emphasis).
		return	rhs is not null &&
				( ignoreTimeLastSeen || TimeLastSeen == rhs.TimeLastSeen ) &&
				SenderEquals( rhs, senderComparison ) &&
				Message.TextValue.Equals( rhs.Message.TextValue );
	}

	public bool SenderEquals( NpcMessageInfo? rhs, SenderComparisonType senderComparison = SenderComparisonType.ObjectID )
	{
		return rhs is not null && senderComparison switch
		{
			SenderComparisonType.ObjectID => SpeakerObjectID.Equals( rhs.SpeakerObjectID ),
			SenderComparisonType.BaseID => SpeakerBaseID.Equals( rhs.SpeakerBaseID ),
			SenderComparisonType.NameID => SpeakerNameID.Equals( rhs.SpeakerNameID ),
			SenderComparisonType.NameText => SpeakerName.TextValue.Equals( rhs.SpeakerName.TextValue ),
			SenderComparisonType.Ignore => true,
			_ => SpeakerObjectID.Equals( rhs.SpeakerObjectID ),
		};
	}

	public static bool operator ==( NpcMessageInfo? lhs, NpcMessageInfo? rhs ) => lhs is null ? rhs is null : lhs.Equals( rhs );
	public static bool operator !=( NpcMessageInfo? lhs, NpcMessageInfo? rhs ) => !( lhs == rhs );
	public bool Equals( NpcMessageInfo? rhs ) => this.Equals( rhs, true, SenderComparisonType.ObjectID );
	public override bool Equals( object? obj ) => obj is NpcMessageInfo rhs && this.Equals( rhs );
	public override int GetHashCode() => HashCode.Combine( SpeakerObjectID.GetHashCode(), Message?.GetHashCode() );
}