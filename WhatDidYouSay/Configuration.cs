using System;

using Dalamud.Configuration;
using Dalamud.Plugin;

namespace WhatDidYouSay
{
	[Serializable]
	public class Configuration : IPluginConfiguration
	{
		public Configuration()
		{
		}

		//  Our own configuration options and data.

		//	Need a real backing field on the properties for use with ImGui.
		public bool mSuppressCommandLineResponses = false;
		public bool SuppressCommandLineResponses
		{
			get { return mSuppressCommandLineResponses; }
			set { mSuppressCommandLineResponses = value; }
		}

		public bool mKeepLineBreaks = false;
		public bool KeepLineBreaks
		{
			get { return mKeepLineBreaks; }
			set { mKeepLineBreaks = value; }
		}

		public bool mRepeatsAllowed = false;
		public bool RepeatsAllowed
		{
			get { return mRepeatsAllowed; }
			set { mRepeatsAllowed = value; }
		}

		public bool mRepeatsAllowedInInstance = false;
		public bool RepeatsAllowedInInstance
		{
			get { return mRepeatsAllowedInInstance; }
			set { mRepeatsAllowedInInstance = value; }
		}

		public bool mIgnoreIfAlreadyInChat_NPCDialogue = false;
		public bool IgnoreIfAlreadyInChat_NPCDialogue
		{
			get { return mIgnoreIfAlreadyInChat_NPCDialogue; }
			set { mIgnoreIfAlreadyInChat_NPCDialogue = value; }
		}

		public bool mIgnoreIfAlreadyInChat_NPCDialogueAnnouncements = false;
		public bool IgnoreIfAlreadyInChat_NPCDialogueAnnouncements
		{
			get { return mIgnoreIfAlreadyInChat_NPCDialogueAnnouncements; }
			set { mIgnoreIfAlreadyInChat_NPCDialogueAnnouncements = value; }
		}

		public int mTimeBeforeRepeatsAllowed_Sec = 5;
		public int TimeBeforeRepeatsAllowed_Sec
		{
			get { return mTimeBeforeRepeatsAllowed_Sec; }
			set { mTimeBeforeRepeatsAllowed_Sec = value; }
		}

		public int mTimeBeforeRepeatsAllowedInInstance_Sec = 1;
		public int TimeBeforeRepeatsAllowedInInstance_Sec
		{
			get { return mTimeBeforeRepeatsAllowedInInstance_Sec; }
			set { mTimeBeforeRepeatsAllowedInInstance_Sec = value; }
		}

		public int mMinTimeBetweenChatPrints_mSec = 500;
		public int MinTimeBetweenChatPrints_mSec
		{
			get { return mMinTimeBetweenChatPrints_mSec; }
			set { mMinTimeBetweenChatPrints_mSec= value; }
		}

		//***** TODO: Fix when Dalamud has these chat channels.
		public int mChatChannelToUse = 0x44;   //	Backing field as an int to work with ImGui.
		public Dalamud.Game.Text.XivChatType ChatChannelToUse
		{
			get { return (Dalamud.Game.Text.XivChatType)mChatChannelToUse; }
			set { mChatChannelToUse = (int)value; }
		}

		//  Plugin framework and related convenience functions below.
		public void Initialize( DalamudPluginInterface pluginInterface )
		{
			mPluginInterface = pluginInterface;
		}

		public void Save()
		{
			mPluginInterface.SavePluginConfig( this );
		}

		[NonSerialized]
		protected DalamudPluginInterface mPluginInterface;

		public int Version { get; set; } = 0;
	}
}
