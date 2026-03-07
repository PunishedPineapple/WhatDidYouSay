using System;
using System.Collections.Generic;

using Dalamud.Configuration;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.Text;
using Dalamud.Plugin;

using Lumina.Data.Parsing.Scd;
using Lumina.Excel.Sheets;

namespace WhatDidYouSay;

//	IMPORTANT: This class (and its serializable members) must be public in order to be properly serialized and deserialized.

[Serializable]
public sealed class Configuration : IPluginConfiguration
{
	#region Interface

	public void Initialize( IDalamudPluginInterface pluginInterface ) => mPluginInterface = pluginInterface;

	public void Save() => mPluginInterface?.SavePluginConfig( this );

	public int Version { get; set; } = 0;

	[NonSerialized]
	private IDalamudPluginInterface? mPluginInterface;

	#endregion

	#region Options

	//	Need a real backing field on the properties for use with ImGui.
	public bool KeepLineBreaks = false;
	public bool RepeatsAllowed = true;
	public bool RepeatsAllowedInInstance = true;
	public int TimeBeforeRepeatsAllowed_Sec = 30;
	public int TimeBeforeRepeatsAllowedInInstance_Sec = 15;
	public bool RepeatDetectionIsCumulative = true;
	public bool RepeatDetectionIsCumulative_InInstance = true;
	public int MinTimeBetweenChatPrints_mSec = 500;
	public float MinBalloonTime_Sec = 0f;
	public float MinBalloonTimeInInstance_Sec = 0f;
	public bool UseCuratedNames = true;
	public bool UseProceduralNames = true;
	public bool ProceduralNameReduceGender = false;
	public bool ProceduralNameIgnoreRegion = false;
	public string DefaultSenderName = "NPC";
	public bool ShowNpcIdInChatMessage_ForRealName = false;
	public bool ShowNpcIdInChatMessage_ForCuratedName = false;
	public bool ShowNpcIdInChatMessage_ForProceduralName = false;
	public bool ShowNpcIdInChatMessage_ForNoName = false;

	public SortedDictionary<UInt32, ZoneSpecificConfig> mZoneConfigOverrideDict = new();

	//	Backing fields as ints to work with ImGui.
	internal int mChatChannelToUse = (int)XivChatType.NPCDialogueAnnouncements;
	internal int mRepeatDetectionSenderComparisonType_ENpc = (int)SenderComparisonType.BaseId;
	internal int mRepeatDetectionSenderComparisonType_ENpc_InInstance = (int)SenderComparisonType.BaseId;
	internal int mRepeatDetectionSenderComparisonType_BNpc = (int)SenderComparisonType.NameId_FallbackBaseId;
	internal int mRepeatDetectionSenderComparisonType_BNpc_InInstance = (int)SenderComparisonType.NameId_FallbackBaseId;
	internal int mProceduralNameRaceKind = (int)RaceNameKind.Tribe;

	public XivChatType ChatChannelToUse
	{
		get => (XivChatType)mChatChannelToUse;
		set => mChatChannelToUse = (int)value;
	}

	public SenderComparisonType RepeatDetectionSenderComparisonType_ENpc
	{
		get => (SenderComparisonType)mRepeatDetectionSenderComparisonType_ENpc;
		set => mRepeatDetectionSenderComparisonType_ENpc = (int)value;
	}

	public SenderComparisonType RepeatDetectionSenderComparisonType_ENpc_InInstance
	{
		get => (SenderComparisonType)mRepeatDetectionSenderComparisonType_ENpc_InInstance;
		set => mRepeatDetectionSenderComparisonType_ENpc_InInstance = (int)value;
	}

	public SenderComparisonType RepeatDetectionSenderComparisonType_BNpc
	{
		get => (SenderComparisonType)mRepeatDetectionSenderComparisonType_BNpc;
		set => mRepeatDetectionSenderComparisonType_BNpc = (int)value;
	}

	public SenderComparisonType RepeatDetectionSenderComparisonType_BNpc_InInstance
	{
		get => (SenderComparisonType)mRepeatDetectionSenderComparisonType_BNpc_InInstance;
		set => mRepeatDetectionSenderComparisonType_BNpc_InInstance = (int)value;
	}

	public RaceNameKind ProceduralNameRaceKind
	{
		get => (RaceNameKind)mProceduralNameRaceKind;
		set => mProceduralNameRaceKind = (int)value;
	}

	#endregion

	#region Helpers

	internal TimeSpan TimeBeforeRepeatsAllowed => TimeSpan.FromSeconds( TimeBeforeRepeatsAllowed_Sec );
	internal TimeSpan TimeBeforeRepeatsAllowed_InInstance => TimeSpan.FromSeconds( TimeBeforeRepeatsAllowedInInstance_Sec );
	internal TimeSpan MinTimeBetweenChatPrints => TimeSpan.FromMilliseconds( MinTimeBetweenChatPrints_mSec );

	internal TimeSpan GetTimeBeforeRepeatsAllowed( UInt32 territoryType, bool inInstance )
	{
		var baseTimeBeforeRepeatsAllowed = inInstance ? TimeBeforeRepeatsAllowed_InInstance : TimeBeforeRepeatsAllowed;

		if( mZoneConfigOverrideDict?.TryGetValue( territoryType, out var zoneConfig ) == true )
		{
			bool repeatsAllowed = zoneConfig.InheritRepeatsAllowed ? RepeatsAllowed : zoneConfig.RepeatsAllowed;
			if( !repeatsAllowed ) return TimeSpan.MaxValue;
			else return zoneConfig.InheritTimeBeforeRepeatsAllowed ? baseTimeBeforeRepeatsAllowed : zoneConfig.TimeBeforeRepeatsAllowed;
		}
		else if( RepeatsAllowed )
		{
			return baseTimeBeforeRepeatsAllowed;
		}
		else
		{
			return TimeSpan.MaxValue;
		}
	}

	internal float GetMinBalloonTime_Sec( UInt32 territoryType, bool inInstance )
	{
		var baseMinBalloonTime_Sec = inInstance ? MinBalloonTimeInInstance_Sec : MinBalloonTime_Sec;

		if( mZoneConfigOverrideDict?.TryGetValue( DalamudAPI.ClientState.TerritoryType, out var zoneConfig ) == true )
		{
			return zoneConfig.InheritMinBalloonTime ? baseMinBalloonTime_Sec : zoneConfig.MinBalloonTime_Sec;
		}
		else
		{
			return baseMinBalloonTime_Sec;
		}
	}

	internal SenderComparisonType GetSenderComparisonType( UInt32 territoryType, bool inInstance, ObjectKind objectKind )
	{
		return objectKind switch
		{
			ObjectKind.EventNpc => GetSenderComparisonType_ENpc( territoryType, inInstance ),
			ObjectKind.BattleNpc => GetSenderComparisonType_BNpc( territoryType, inInstance ),
			_ => SenderComparisonType.ObjectId,
		};
	}

	private SenderComparisonType GetSenderComparisonType_ENpc( UInt32 territoryType, bool inInstance )
	{
		var baseSenderComparisonType = inInstance ? RepeatDetectionSenderComparisonType_ENpc_InInstance : RepeatDetectionSenderComparisonType_ENpc;

		if( mZoneConfigOverrideDict?.TryGetValue( DalamudAPI.ClientState.TerritoryType, out var zoneConfig ) == true )
		{
			return zoneConfig.InheritSenderComparisonType_ENpc ? baseSenderComparisonType : zoneConfig.SenderComparisonType_ENpc;
		}
		else
		{
			return baseSenderComparisonType;
		}
	}

	private SenderComparisonType GetSenderComparisonType_BNpc( UInt32 territoryType, bool inInstance )
	{
		var baseSenderComparisonType = inInstance ? RepeatDetectionSenderComparisonType_BNpc_InInstance : RepeatDetectionSenderComparisonType_BNpc;

		if( mZoneConfigOverrideDict?.TryGetValue( DalamudAPI.ClientState.TerritoryType, out var zoneConfig ) == true )
		{
			return zoneConfig.InheritSenderComparisonType_BNpc ? baseSenderComparisonType : zoneConfig.SenderComparisonType_BNpc;
		}
		else
		{
			return baseSenderComparisonType;
		}
	}

	#endregion
}
