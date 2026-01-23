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
		SpeakerObjectId = speakerObjectID;
		SpeakerKind = speakerKind;
		SpeakerBaseId = speakerBaseID;
		SpeakerNameId = speakerNameID;
		SpeakerName = speakerName;
		Message = message;
	}

	public GameObjectId SpeakerObjectId { get; init; } = 0xE0000000;
	public ObjectKind SpeakerKind { get; init; } = ObjectKind.None;
	public UInt32 SpeakerBaseId { get; init; } = 0;
	public UInt32 SpeakerNameId { get; init; } = 0;
	public SeString SpeakerName { get; init; } = SeString.Empty;
	public SeString Message { get; init; } = SeString.Empty;
	public DateTimeOffset TimeFirstSeen { get; protected set; }
	public DateTimeOffset TimeLastSeen { get; protected set; }
	public bool HasBeenPrinted { get; protected set; }
	public bool IsExpired( TimeSpan lifetime ) => DateTimeOffset.UtcNow - TimeLastSeen > lifetime;

	public void FlagMessageSeenAgain() => TimeLastSeen = DateTimeOffset.UtcNow;
	public void FlagGameHandledPrinting() => HasBeenPrinted = true;
	public bool PrintToChatLog( NpcNameDatabase customNameData, Configuration currentConfig )
	{
		//	Rate limit this as a last resort in case we messed up.
		var currentTime = DateTimeOffset.UtcNow;
		if( currentTime - LastTimeChatPrinted >= currentConfig.MinTimeBetweenChatPrints )
		{
			//	Make a copy so that we can do some formatting without poisoning the history.
			var msg = Message.Duplicate();

			//	Remove line breaks if desired.
			if( !currentConfig.KeepLineBreaks )
			{
				msg.RemoveLineBreaksInPlace();
			}

			//	Get as reasonable a speaker name we can.
			var speakerName = GetBestAvailableSenderName( customNameData, currentConfig, out var hasRealName, out var hasCuratedName, out var hasProceduralName );

			//	Append the object ID(s) if desired.
			if( currentConfig.ShowNPCIDInChatMessage_ForRealName && hasRealName ||
				currentConfig.ShowNPCIDInChatMessage_ForCuratedName && hasCuratedName ||
				currentConfig.ShowNPCIDInChatMessage_ForProceduralName && hasProceduralName ||
				currentConfig.ShowNPCIDInChatMessage_ForNoName && !( hasRealName || hasCuratedName || hasProceduralName ) )
			{
				string kindStr = SpeakerKind switch
				{
					ObjectKind.BattleNpc => "BNpc",
					ObjectKind.EventNpc => "ENpc",
					_ => "Unk",
				};

				string nameIdStr = SpeakerKind switch
				{
					ObjectKind.BattleNpc => $", {SpeakerNameId}",
					_ => string.Empty,
				};

				msg.Payloads.Add( new UIForegroundPayload( 4 ) );
				msg.Payloads.Add( new TextPayload( $" ({kindStr} {SpeakerBaseId}{nameIdStr})" ) );
				msg.Payloads.Add( new UIForegroundPayload( 0 ) );
			}

			DalamudAPI.ChatGui.Print( new()
			{
				Type = currentConfig.ChatChannelToUse,
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

	protected SeString GetBestAvailableSenderName( NpcNameDatabase customNameData, Configuration currentConfig, out bool hasRealName, out bool hasCuratedName, out bool hasProceduralName )
	{
		hasRealName = false;
		hasCuratedName = false;
		hasProceduralName = false;

		//	Game-given name always takes precedence.
		if( !string.IsNullOrWhiteSpace( SpeakerName?.TextValue ) )
		{
			hasRealName = true;
			return SpeakerName;
		}

		//	Custom and procedural names.
		if( SpeakerKind == ObjectKind.BattleNpc )
		{
			if( currentConfig.UseCuratedNames &&
				customNameData.TryGetBNpcName( SpeakerBaseId, SpeakerNameId, out var customName ) &&
				customName.Length > 0 )
			{
				hasCuratedName = true;
				return customName;
			}
			else if( currentConfig.UseProceduralNames &&
				NpcNameGenerator.TryGenerateNameFromBNpcId( SpeakerBaseId, out var computedName ) )
			{
				hasProceduralName = true;
				return computedName;
			}
		}
		else if( SpeakerKind == ObjectKind.EventNpc )
		{
			if( currentConfig.UseCuratedNames &&
				customNameData.TryGetENpcName( SpeakerBaseId, out var customName ) &&
				customName.Length > 0 )
			{
				hasCuratedName = true;
				return customName;
			}
			else if( currentConfig.UseProceduralNames &&
				NpcNameGenerator.TryGenerateNameFromENpcId( SpeakerBaseId, out var computedName ) )
			{
				hasProceduralName = true;
				return computedName;
			}
		}

		//	Configured default name.
		if( currentConfig.DefaultSenderName is string defaultSender &&
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
		string str = $"Speaker ID: 0x{(ulong)SpeakerObjectId:X}, Speaker Base ID: {SpeakerBaseId}, Speaker Name ID: {SpeakerNameId}, Speaker Kind: {SpeakerKind}";
		str += $"\r\n[{TimeFirstSeen.ToLocalTime():HH:mm:ss}]{SpeakerName.TextValue}: {Message.TextValue}";
		str += $"\r\nLast Seen: {TimeLastSeen.ToLocalTime():HH:mm:ss} ";
		return str;
	}

	public bool Equals( NpcMessageInfo? rhs, bool ignoreTimeLastSeen = true, SenderComparisonType senderComparison = SenderComparisonType.ObjectID )
	{
		//***** TODO: It is conceivable that we might have messages that differ only in non-text payloads (i.e., a repeat with italics for emphasis).
		return rhs is not null &&
				( ignoreTimeLastSeen || TimeLastSeen == rhs.TimeLastSeen ) &&
				SenderEquals( rhs, senderComparison ) &&
				Message.TextValue.Equals( rhs.Message.TextValue );
	}

	public bool SenderEquals( NpcMessageInfo? rhs, SenderComparisonType senderComparison = SenderComparisonType.ObjectID )
	{
		if( rhs is null ||
			senderComparison == SenderComparisonType.NeverEqual ||
			SpeakerKind != rhs.SpeakerKind )
		{
			return false;
		}

		return senderComparison switch
		{
			SenderComparisonType.ObjectID => SpeakerObjectId.Equals( rhs.SpeakerObjectId ),
			SenderComparisonType.BaseID => SpeakerBaseId.Equals( rhs.SpeakerBaseId ),
			SenderComparisonType.NameID_FallbackBaseID => SpeakerNameId == 0 || rhs.SpeakerNameId == 0 ? SenderEquals( rhs, SenderComparisonType.BaseID ) : SpeakerNameId.Equals( rhs.SpeakerNameId ),
			SenderComparisonType.NameID_FallbackText => SpeakerNameId == 0 || rhs.SpeakerNameId == 0 ? SenderEquals( rhs, SenderComparisonType.NameText ) : SpeakerNameId.Equals( rhs.SpeakerNameId ),
			SenderComparisonType.NameText => SpeakerName.TextValue.Length == 0 && rhs.SpeakerName.TextValue.Length == 0 ? SenderEquals( rhs, SenderComparisonType.BaseID ) : SpeakerName.TextValue.Equals( rhs.SpeakerName.TextValue ),
			SenderComparisonType.NeverEqual => false,
			_ => SpeakerObjectId.Equals( rhs.SpeakerObjectId ),
		};
	}

	public static bool operator ==( NpcMessageInfo? lhs, NpcMessageInfo? rhs ) => lhs is null ? rhs is null : lhs.Equals( rhs );
	public static bool operator !=( NpcMessageInfo? lhs, NpcMessageInfo? rhs ) => !( lhs == rhs );
	public bool Equals( NpcMessageInfo? rhs ) => this.Equals( rhs, true, SenderComparisonType.ObjectID );
	public override bool Equals( object? obj ) => obj is NpcMessageInfo rhs && this.Equals( rhs );
	public override int GetHashCode() => HashCode.Combine( SpeakerObjectId.GetHashCode(), Message?.GetHashCode() );
}