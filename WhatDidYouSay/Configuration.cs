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
	public bool SuppressCommandLineResponses = false;
	public bool KeepLineBreaks = false;
	public bool RepeatsAllowed = true;
	public bool RepeatsAllowedInInstance = true;
	public bool IgnoreIfAlreadyInChat_NPCDialogue = false;
	public bool IgnoreIfAlreadyInChat_NPCDialogueAnnouncements = true;
	public bool RepeatDetectionIgnoresSenderID = true;
	public bool RepeatDetectionIgnoresSenderID_InInstance = true;
	public int TimeBeforeRepeatsAllowed_Sec = 30;
	public int TimeBeforeRepeatsAllowedInInstance_Sec = 5;
	public int MinTimeBetweenChatPrints_mSec = 500;
	public string DefaultSenderName = "NPC";

	//	Backing field as an int to work with ImGui.
	internal int mChatChannelToUse = (int)XivChatType.NPCDialogueAnnouncements;
	public XivChatType ChatChannelToUse
	{
		get => (XivChatType)mChatChannelToUse;
		set => mChatChannelToUse = (int)value;
	}

	public ushort NpcMessageTextColor = 0;
	public ushort NpcMessageGlowColor = 0;

	public ushort SenderTextColor_Unknown = 0;
	public ushort SenderGlowColor_Unknown = 0;

	public bool IgnorePlayerCharacterBubbles = false;

	public bool DEBUG_NotifyOnUnknownParam3Bubble = false;

	public SortedDictionary<UInt32, ZoneSpecificConfig> mZoneConfigOverrideDict = new();

	#endregion

	#region Helpers

	internal TimeSpan TimeBeforeRepeatsAllowed => TimeSpan.FromSeconds( TimeBeforeRepeatsAllowed_Sec );
	internal TimeSpan TimeBeforeRepeatsAllowed_InInstance => TimeSpan.FromSeconds( TimeBeforeRepeatsAllowedInInstance_Sec );
	internal TimeSpan MinTimeBetweenChatPrints => TimeSpan.FromMilliseconds( MinTimeBetweenChatPrints_mSec );

	#endregion
}
