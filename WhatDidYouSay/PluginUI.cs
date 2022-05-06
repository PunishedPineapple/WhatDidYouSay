﻿using System;
using System.IO;
using System.Numerics;

using CheapLoc;

using Dalamud.Plugin;

using ImGuiNET;

namespace WhatDidYouSay
{
	// It is good to have this be disposable in general, in case you ever need it
	// to do any cleanup
	public class PluginUI : IDisposable
	{
		//	Construction
		public PluginUI( Plugin plugin, Configuration configuration, DalamudPluginInterface pluginInterface )
		{
			mPlugin = plugin;
			mConfiguration = configuration;
			mPluginInterface = pluginInterface;
		}

		//	Destruction
		public void Dispose()
		{
		}

		public void Initialize()
		{
		}

		public void Draw()
		{
			//	Draw the sub-windows.
			DrawSettingsWindow();
			DrawDebugWindow();
		}

		protected void DrawSettingsWindow()
		{
			if( !SettingsWindowVisible )
			{
				return;
			}

			if( ImGui.Begin( Loc.Localize( "Window Title: Config", "\"Say What?\" Settings" ) + "###\"Say What?\" Settings",
				ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse ) )
			{
				//***** TODO: Revisit this when Dalamud has the right chat type enums.
				ImGui.Text( Loc.Localize( "Config Option: Log message channel.", "Log channel for output:" ) );
				int selectedIndex = mConfiguration.mChatChannelToUse == 0x44 ? 1 : 0;
				string[] comboItems = new string[]
				{
					"NPC Dialogue",
					"NPC Dialogue (Announcements)"
				};
				ImGui.Combo( "###Chat Channel Dropdown", ref selectedIndex, comboItems, comboItems.Length );
				mConfiguration.mChatChannelToUse = selectedIndex == 1 ? 0x44 : 0x3D;

				ImGui.Spacing();
				ImGui.Spacing();
				ImGui.Spacing();
				ImGui.Spacing();
				ImGui.Spacing();

				ImGui.Text( Loc.Localize( "Config Option: Minimum time between log messages.", "Minimum time between log messages (msec):" ) );
				ImGui.SliderInt( "###Minimum time between any log messages.", ref mConfiguration.mMinTimeBetweenChatPrints_mSec, 0, 2000 );

				ImGui.Spacing();
				ImGui.Spacing();
				ImGui.Spacing();
				ImGui.Spacing();
				ImGui.Spacing();

				ImGui.Checkbox( Loc.Localize( "Config Option: Keep Line Breaks", "Keep line breaks." ) + "###Keep line breaks.", ref mConfiguration.mKeepLineBreaks );

				ImGui.Spacing();
				ImGui.Spacing();
				ImGui.Spacing();
				ImGui.Spacing();
				ImGui.Spacing();

				ImGui.Text( Loc.Localize( "Config Section: Prevent Duplicate Messages", "Prevent duplicate messages from:" ) );
				ImGuiUtils.HelpMarker( Loc.Localize( "Help: Prevent Duplicate Messages", "Checking the box for the specified chat channel ensures that NPC speech bubbles with the same content as a message already printed by the game in that channel will not be duplicated.  The usefulness of these options depends upon how your chat filters are configured." ) );
				ImGui.Indent();
				ImGui.Checkbox( Loc.Localize( "Config Option: Prevent Duplicate Messages (NPC Dialogue)", "NPC Dialogue" ) + "###Prevent Duplicate Messages (NPC Dialogue)", ref mConfiguration.mIgnoreIfAlreadyInChat_NPCDialogue );
				ImGui.Checkbox( Loc.Localize( "Config Option: Prevent Duplicate Messages (NPC Dialogue (Announcements))", "NPC Dialogue (Announcements)" ) + "###Prevent Duplicate Messages (NPC Dialogue (Announcements))", ref mConfiguration.mIgnoreIfAlreadyInChat_NPCDialogueAnnouncements );
				ImGui.Unindent();

				ImGui.Spacing();
				ImGui.Spacing();
				ImGui.Spacing();
				ImGui.Spacing();
				ImGui.Spacing();

				ImGui.Text( Loc.Localize( "Config Section: Overworld", "Overworld:" ) );
				ImGui.Indent();
				ImGui.Checkbox( Loc.Localize( "Config Option: Allow repeated speech to print to log (overworld).", "Allow repeated speech to print to log." ) + "###Allow repeated speech to print to log (overworld).", ref mConfiguration.mRepeatsAllowed );
				ImGuiUtils.HelpMarker( Loc.Localize( "Help: Config Options Allow Repeated Speech", "If this is not checked, a given NPC speech bubble will never be repeated in the chat log until you change zones and come back." ) );
				if( mConfiguration.RepeatsAllowed )
				{
					ImGui.Text( Loc.Localize( "Config Option: Time before repeated speech can be printed again (overworld).", "Time before repeated speech can be printed again (sec):" ) );
					ImGui.SliderInt( "###Time before the same speech can be printed again (overworld).", ref mConfiguration.mTimeBeforeRepeatsAllowed_Sec, 1, 600 );
				}
				ImGui.Unindent();

				ImGui.Spacing();
				ImGui.Spacing();
				ImGui.Spacing();
				ImGui.Spacing();
				ImGui.Spacing();

				ImGui.Text( Loc.Localize( "Config Section: Instance", "In Instance:" ) );
				ImGui.Indent();
				ImGui.Checkbox( Loc.Localize( "Config Option: Allow repeated speech to print to log (in-instance).", "Allow repeated speech to print to log." ) + "###Allow repeated speech to print to log (InInstance)", ref mConfiguration.mRepeatsAllowedInInstance );
				ImGuiUtils.HelpMarker( Loc.Localize( "Help: Config Options Allow Repeated Speech", "If this is not checked, a given NPC speech bubble will never be repeated in the chat log until you change zones and come back." ) );
				if( mConfiguration.RepeatsAllowedInInstance )
				{
					ImGui.Text( Loc.Localize( "Config Option: Time before repeated speech can be printed again (in-instance).", "Time before repeated speech can be printed again (sec):" ) );
					ImGui.SliderInt( "###Time before the same speech can be printed again (in instance).", ref mConfiguration.mTimeBeforeRepeatsAllowedInInstance_Sec, 1, 600 );
				}
				ImGui.Unindent();

				ImGui.Spacing();
				ImGui.Spacing();
				ImGui.Spacing();
				ImGui.Spacing();
				ImGui.Spacing();

				/*if( ImGui.Button( Loc.Localize( "Button: Save", "Save" ) + "###Save Button" ) )
				{
					mConfiguration.Save();
				}
				ImGui.SameLine();*/
				if( ImGui.Button( Loc.Localize( "Button: Save and Close", "Save and Close" ) + "###Save and Close" ) )
				{
					mConfiguration.Save();
					SettingsWindowVisible = false;
				}
			}

			ImGui.End();
		}

		protected void DrawDebugWindow()
		{
			if( !DebugWindowVisible )
			{
				return;
			}

			//	Draw the window.
			ImGui.SetNextWindowSize( new Vector2( 1340, 568 ) * ImGui.GetIO().FontGlobalScale, ImGuiCond.FirstUseEver );
			ImGui.SetNextWindowSizeConstraints( new Vector2( 375, 340 ) * ImGui.GetIO().FontGlobalScale, new Vector2( float.MaxValue, float.MaxValue ) );
			if( ImGui.Begin( Loc.Localize( "Window Title: Debug Data", "\"Say What?\" Debug Data" ) + "###\"Say What?\" Debug Data", ref mDebugWindowVisible ) )
			{
				if( ImGui.Button( "Export Localizable Strings" ) )
				{
					string pwd = Directory.GetCurrentDirectory();
					Directory.SetCurrentDirectory( mPluginInterface.AssemblyLocation.DirectoryName );
					Loc.ExportLocalizable();
					Directory.SetCurrentDirectory( pwd );
				}

				if( ImGui.Button( "Reset seen speech history" ) )
				{
					mPlugin.ClearSpeechBubbleHistory();
				}

				ImGui.Spacing();
				ImGui.Spacing();
				ImGui.Spacing();
				ImGui.Spacing();
				ImGui.Spacing();

				var entries = mPlugin.GetSpeechBubbleInfo_DEBUG();
				foreach( var entry in entries )
				{
					ImGui.Text( $"String: {entry.MessageText}, Speaker: {entry.SpeakerName}, Time Last Seen: {entry.TimeLastSeen_mSec}, Has Been Printed {entry.HasBeenPrinted}" );
					ImGui.Indent();
					foreach( var payload in entry.MessageText.Payloads )
					{
						ImGui.Text( $"Type: {payload.Type}, Contents: {payload.ToString()}" );
					}
					ImGui.Unindent();
				}

				if( ImGui.Button( "Seen chat messages" ) )
				{
				}

				ImGui.Spacing();
				ImGui.Spacing();
				ImGui.Spacing();
				ImGui.Spacing();
				ImGui.Spacing();

				foreach( var entry in mPlugin.GetGameChatInfo_DEBUG() )
				{
					if( ImGui.Button( $"P###btn-chat{entry.GetHashCode()}" ) )
					{
						mPlugin.PrintChatMessage_DEBUG( entry.MessageText, entry.SpeakerName );
					}
					ImGui.SameLine();
					ImGui.Text( $"String: {entry.MessageText}, Speaker: {entry.SpeakerName}, Time Last Seen: {entry.TimeLastSeen_mSec}, Has Been Printed {entry.HasBeenPrinted}" );
				}
			}

			//	We're done.
			ImGui.End();
		}

		protected Plugin mPlugin;
		protected DalamudPluginInterface mPluginInterface;
		protected Configuration mConfiguration;

		//	Need a real backing field on the following properties for use with ImGui.
		protected bool mSettingsWindowVisible = false;
		public bool SettingsWindowVisible
		{
			get { return mSettingsWindowVisible; }
			set { mSettingsWindowVisible = value; }
		}

		protected bool mDebugWindowVisible = false;
		public bool DebugWindowVisible
		{
			get { return mDebugWindowVisible; }
			set { mDebugWindowVisible = value; }
		}
	}
}