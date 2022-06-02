using System;
using System.Globalization;
using System.IO;
using System.Numerics;

using CheapLoc;

using Dalamud.Game.ClientState;
using Dalamud.Plugin;
using Dalamud.Game.Gui;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;

using ImGuiNET;

using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;

namespace WhatDidYouSay
{
	// It is good to have this be disposable in general, in case you ever need it
	// to do any cleanup
	public class PluginUI : IDisposable
	{
		//	Construction
		public PluginUI( Plugin plugin, Configuration configuration, DalamudPluginInterface pluginInterface, ClientState clientState )
		{
			mPlugin = plugin;
			mConfiguration = configuration;
			mPluginInterface = pluginInterface;
			mClientState = clientState;
		}

		//	Destruction
		public void Dispose()
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
				ImGui.Text( Loc.Localize( "Config Option: Log message channel.", "Chat Log channel for output:" ) );
				ImGuiUtils.HelpMarker( Loc.Localize( "Help: Log message channel.", "Make sure that the channel you select here is turned on in your character's log filter settings, or you won't see any messages." ) );
				int selectedIndex = mConfiguration.mChatChannelToUse == 0x44 ? 1 : 0;
				string[] comboItems = new string[]
				{
					GetNPCDialogueChannelName(),
					GetNPCDialogueAnnouncementsChannelName()
				};
				ImGui.Combo( "###Chat Channel Dropdown", ref selectedIndex, comboItems, comboItems.Length );
				mConfiguration.mChatChannelToUse = selectedIndex == 1 ? 0x44 : 0x3D;

				ImGui.Spacing();

				ImGui.Text( Loc.Localize( "Config Option: Minimum time between log messages.", "Minimum time between chat log messages:" ) );
				ImGuiUtils.HelpMarker( Loc.Localize( "Help: Minimum time between log messages.", "If multiple speech bubbles appear on-screen at the same time, this is how long will pass between each one going into the chat log." ) );
				ImGui.SliderInt( "###Minimum time between any log messages.", ref mConfiguration.mMinTimeBetweenChatPrints_mSec, 0, 2000, "%dms" );

				ImGui.Spacing();

				ImGui.Checkbox( Loc.Localize( "Config Option: Keep Line Breaks", "Keep line breaks." ) + "###Keep line breaks.", ref mConfiguration.mKeepLineBreaks );

				ImGui.Spacing();

				ImGui.Text( Loc.Localize( "Config Section: Prevent Duplicate Messages", "Prevent duplicate messages from:" ) );
				ImGuiUtils.HelpMarker( Loc.Localize( "Help: Prevent Duplicate Messages", "In some rare cases, the base game already prints speech bubbles to the chat log.  These settings make the plugin skip outputting to chat in those cases.  Which option(s) you want depends upon how your chat filters are configured, but generally you should check the box that matches the output channel selected above." ) );
				ImGui.Indent();
				ImGui.Checkbox( GetNPCDialogueChannelName() + "###Prevent Duplicate Messages (NPC Dialogue)", ref mConfiguration.mIgnoreIfAlreadyInChat_NPCDialogue );
				ImGui.Checkbox( GetNPCDialogueAnnouncementsChannelName() + "###Prevent Duplicate Messages (NPC Dialogue (Announcements))", ref mConfiguration.mIgnoreIfAlreadyInChat_NPCDialogueAnnouncements );
				ImGui.Unindent();

				ImGui.Spacing();

				ImGui.Text( Loc.Localize( "Config Section: Overworld", "Overworld:" ) );
				ImGui.Indent();
				ImGui.Checkbox( Loc.Localize( "Config Option: Allow repeated speech to print to log.", "Allow repeated speech to print to log." ) + "###Allow repeated speech to print to log (overworld).", ref mConfiguration.mRepeatsAllowed );
				ImGuiUtils.HelpMarker( Loc.Localize( "Help: Config Options Allow Repeated Speech", "If this is not checked, a given NPC speech bubble will never be repeated in the chat log until you change zones and come back." ) );
				if( mConfiguration.RepeatsAllowed )
				{
					ImGui.Text( Loc.Localize( "Config Option: Time before repeated speech can be printed again.", "Time before repeats are allowed:" ) );
					ImGui.SliderInt( "###Time before the same speech can be printed again (overworld).", ref mConfiguration.mTimeBeforeRepeatsAllowed_Sec, 1, 600, "%ds" );
				}
				ImGui.Unindent();

				ImGui.Spacing();

				ImGui.Text( Loc.Localize( "Config Section: Instance", "In Instance:" ) );
				ImGui.Indent();
				ImGui.Checkbox( Loc.Localize( "Config Option: Allow repeated speech to print to log.", "Allow repeated speech to print to log." ) + "###Allow repeated speech to print to log (InInstance)", ref mConfiguration.mRepeatsAllowedInInstance );
				ImGuiUtils.HelpMarker( Loc.Localize( "Help: Config Options Allow Repeated Speech", "If this is not checked, a given NPC speech bubble will never be repeated in the chat log until you change zones and come back." ) );
				if( mConfiguration.RepeatsAllowedInInstance )
				{
					ImGui.Text( Loc.Localize( "Config Option: Time before repeated speech can be printed again.", "Time before repeats are allowed:" ) );
					ImGui.SliderInt( "###Time before the same speech can be printed again (in instance).", ref mConfiguration.mTimeBeforeRepeatsAllowedInInstance_Sec, 1, 600, "%ds" );
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

		protected unsafe void DrawDebugWindow()
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

				ImGui.Checkbox( "Disable Automatic Chat History Clearing", ref mPlugin.DisableGameChatDataClearing_DEBUG );
				ImGui.Separator();

				ImGui.InputText( "Chat Param (hex)", ref mCurrentChatParameter_DEBUG, 16 );
				ImGui.InputText( "Chat Sender", ref mCurrentChatSender_DEBUG, 64 );


				if( ImGui.BeginCombo( "Chat Type", mCurrentChatTypeSelection_DEBUG.ToString() ) )
				{
					foreach( Dalamud.Game.Text.XivChatType entry in Enum.GetValues( typeof( Dalamud.Game.Text.XivChatType ) ) )
					{
						if( ImGui.Selectable( entry.ToString() ) ) mCurrentChatTypeSelection_DEBUG = entry;
					}
					ImGui.EndCombo();
				}

				if( ImGui.Button( "Send Test Message" ) )
				{
					long chatParam = 0;
					long.TryParse( mCurrentChatParameter_DEBUG, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out chatParam );
					mPlugin.PrintTestChatMessage_DEBUG( "Test message", mCurrentChatSender_DEBUG, mCurrentChatTypeSelection_DEBUG, new( chatParam ) );
				}

				ImGui.Separator();

				ImGui.Spacing();
				ImGui.Spacing();
				ImGui.Spacing();
				ImGui.Spacing();
				ImGui.Spacing();

				ImGui.TextWrapped( "DON'T FUCK THIS UP OR YOU CAN SEND INVALID DATA TO THE SERVER WHEN RIGHT CLICKING A PLAYER IN THE CHAT LOG." );
				ImGui.InputText( "Content ID (hex)", ref mContentIDToAttach_DEBUG, 16 );
				ImGui.InputText( "Message Index", ref mMessageIndexToWhichToAttach_DEBUG, 8 );
				ImGui.InputText( "Server ID", ref mWorldIDToAttach_DEBUG, 8 );
				if( ImGui.BeginCombo( "Chat Type###ChatTypeToAttach", mChatTypeToAttach_DEBUG.ToString() ) )
				{
					foreach( Dalamud.Game.Text.XivChatType entry in Enum.GetValues( typeof( Dalamud.Game.Text.XivChatType ) ) )
					{
						if( ImGui.Selectable( entry.ToString() ) ) mChatTypeToAttach_DEBUG = entry;
					}
					ImGui.EndCombo();
				}

				if( ImGui.Button( "Attach CID To Message" ) )
				{
					UInt64 contentID = 0;
					UInt64.TryParse( mContentIDToAttach_DEBUG, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out contentID );


					UInt32 messageIndex = 0;
					UInt32.TryParse( mMessageIndexToWhichToAttach_DEBUG, out messageIndex );

					UInt16 serverID = 0;
					UInt16.TryParse( mWorldIDToAttach_DEBUG, out serverID );

					mPlugin.AttachCIDToMessage( contentID, messageIndex, serverID, (UInt16)mChatTypeToAttach_DEBUG );
				}

				ImGui.Separator();

				ImGui.Spacing();
				ImGui.Spacing();
				ImGui.Spacing();
				ImGui.Spacing();
				ImGui.Spacing();

				if( ImGui.Button( "Reset seen speech history" ) )
				{
					mPlugin.ClearSpeechBubbleHistory();
				}

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

				ImGui.Separator();

				ImGui.Spacing();
				ImGui.Spacing();
				ImGui.Spacing();
				ImGui.Spacing();
				ImGui.Spacing();

				if( ImGui.Button( "Reset seen chat history" ) )
				{
					mPlugin.ClearGameChatHistory();
				}

				ImGui.Spacing();

				foreach( var entry in mPlugin.GetGameChatInfo_DEBUG() )
				{
					if( ImGui.Button( $"P###btn-chat{entry.GetHashCode()}" ) )
					{
						mPlugin.PrintChatMessage_DEBUG( entry.MessageText, entry.SpeakerName );

						//***** TESTING
						mPlugin.PrintTestChatMessage_DEBUG( entry.MessageText, entry.SpeakerName, mCurrentChatTypeSelection_DEBUG, IntPtr.Zero );
					}
					ImGui.SameLine();
					ImGui.Text( $"String: {entry.MessageText}, Speaker: {entry.SpeakerName}, Time Last Seen: {entry.TimeLastSeen_mSec}, Has Been Printed {entry.HasBeenPrinted}" );
				}
			}

			//	We're done.
			ImGui.End();
		}

		//	I wanted to get these from the sheets, but the relevant sheet (LogFilter) didn't seem to reliably load in time for some reason.
		protected string GetNPCDialogueChannelName()
		{
			return mClientState.ClientLanguage switch
			{
				Dalamud.ClientLanguage.Japanese => "NPC会話",
				Dalamud.ClientLanguage.English => "NPC Dialogue",
				Dalamud.ClientLanguage.German => "NPC-Gespräche",
				Dalamud.ClientLanguage.French => "Dialogues des PNJ",
				_ => "NPC Dialogue"
			};
		}

		protected string GetNPCDialogueAnnouncementsChannelName()
		{
			return mClientState.ClientLanguage switch
			{
				Dalamud.ClientLanguage.Japanese => "NPC会話（アナウンス）",
				Dalamud.ClientLanguage.English => "NPC Dialogue (Announcements)",
				Dalamud.ClientLanguage.German => "Nachrichten von NPCs",
				Dalamud.ClientLanguage.French => "Annonces des PNJ",
				_ => "NPC Dialogue (Announcements)"
			};
		}

		protected Plugin mPlugin;
		protected DalamudPluginInterface mPluginInterface;
		protected Configuration mConfiguration;
		protected ClientState mClientState;

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

		internal Dalamud.Game.Text.XivChatType mCurrentChatTypeSelection_DEBUG = Dalamud.Game.Text.XivChatType.Say;
		internal string mCurrentChatParameter_DEBUG = "";
		internal string mCurrentChatSender_DEBUG = "";
		internal string mContentIDToAttach_DEBUG = "";
		internal string mMessageIndexToWhichToAttach_DEBUG = "";
		internal string mWorldIDToAttach_DEBUG = "";
		internal Dalamud.Game.Text.XivChatType mChatTypeToAttach_DEBUG = Dalamud.Game.Text.XivChatType.Say;
	}
}