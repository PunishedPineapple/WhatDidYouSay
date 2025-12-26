using System;
using System.Collections.Generic;

using Dalamud.Configuration;
using Dalamud.Game.Text;
using Dalamud.Plugin;

namespace WhatDidYouSay;

//	IMPORTANT: This class (and its serializable members) must be public in order to be properly serialized and deserialized.

[Serializable]
public sealed class Configuration : IPluginConfiguration
{
	#region Interface

	public void Initialize( IDalamudPluginInterface pluginInterface ) => mPluginInterface = pluginInterface;

	public void Save() => mPluginInterface.SavePluginConfig( this );

	public int Version { get; set; } = 0;

	[NonSerialized]
	private IDalamudPluginInterface mPluginInterface;

	#endregion

	#region Options

	//	Need a real backing field on the properties for use with ImGui.
	public bool KeepLineBreaks = false;
	public bool RepeatsAllowed = true;
	public bool RepeatsAllowedInInstance = true;
	public int TimeBeforeRepeatsAllowed_Sec = 30;
	public int TimeBeforeRepeatsAllowedInInstance_Sec = 5;
	public int MinTimeBetweenChatPrints_mSec = 500;
	public float MinBalloonTime_Sec = 0f;
	public float MinBalloonTimeInInstance_Sec = 0f;
	public bool UseCuratedNames = true;
	public bool UseProceduralNames = true;
	public string DefaultSenderName = "NPC";

	//	Backing fields as ints to work with ImGui.
	internal int mChatChannelToUse = (int)XivChatType.NPCDialogueAnnouncements;
	internal int mRepeatDetectionSenderComparisonType = (int)SenderComparisonType.ObjectID;
	internal int mRepeatDetectionSenderComparisonType_InInstance = (int)SenderComparisonType.ObjectID;

	public XivChatType ChatChannelToUse
	{
		get => (XivChatType)mChatChannelToUse;
		set => mChatChannelToUse = (int)value;
	}

	public SenderComparisonType RepeatDetectionSenderComparisonType
	{
		get => (SenderComparisonType)mRepeatDetectionSenderComparisonType;
		set => mRepeatDetectionSenderComparisonType = (int)value;
	}

	public SenderComparisonType RepeatDetectionSenderComparisonType_InInstance
	{
		get => (SenderComparisonType)mRepeatDetectionSenderComparisonType;
		set => mRepeatDetectionSenderComparisonType = (int)value;
	}

	public bool ShowNPCIDForUnnamedNPCs = false;
	public bool ShowNPCIDInChatMessage = false;

	public bool DEBUG_NotifyOnParam3High = false;

	public SortedDictionary<UInt32, ZoneSpecificConfig> mZoneConfigOverrideDict = new();

	#endregion

	#region Helpers

	internal TimeSpan TimeBeforeRepeatsAllowed => TimeSpan.FromSeconds( TimeBeforeRepeatsAllowed_Sec );
	internal TimeSpan TimeBeforeRepeatsAllowed_InInstance => TimeSpan.FromSeconds( TimeBeforeRepeatsAllowedInInstance_Sec );
	internal TimeSpan MinTimeBetweenChatPrints => TimeSpan.FromMilliseconds( MinTimeBetweenChatPrints_mSec );

	#endregion
}
