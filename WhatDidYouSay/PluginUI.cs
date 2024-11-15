using System;
using System.IO;
using System.Numerics;

using CheapLoc;

using Dalamud.Game.Text;
using Dalamud.Plugin;

using ImGuiNET;

namespace WhatDidYouSay;

public class PluginUI : IDisposable
{
	//	Construction
	public PluginUI( Plugin plugin, Configuration configuration, IDalamudPluginInterface pluginInterface )
	{
		mPlugin = plugin;
		mConfiguration = configuration;
		mPluginInterface = pluginInterface;

		mDefaultSenderNameConfigString = mConfiguration.DefaultSenderName;
	}

	//	Destruction
	public void Dispose()
	{
	}

	public void Draw()
	{
		//	Draw the sub-windows.
		DrawSettingsWindow();
		DrawSettingsWindow_ZoneOverrides();
		DrawDebugWindow();
	}

	protected void DrawSettingsWindow()
	{
		if( !SettingsWindowVisible )
		{
			return;
		}

		if( ImGui.Begin( Loc.Localize( "Window Title: Config", "\"Say What?\" Settings" ) + "###\"Say What?\" Settings", ref mSettingsWindowVisible,
			ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse ) )
		{
			ImGui.Text( Loc.Localize( "Config Option: Log message channel.", "Chat Log channel for output:" ) );
			ImGuiUtils.HelpMarker( Loc.Localize( "Help: Log message channel.", "Make sure that the channel you select here is turned on in your character's log filter settings, or you won't see any messages." ) );
			string currentChannelName = mConfiguration.ChatChannelToUse switch
			{
				XivChatType.NPCDialogue => GetNPCDialogueChannelName(),
				XivChatType.NPCDialogueAnnouncements => GetNPCDialogueAnnouncementsChannelName(),
				_ => "INVALID CHANNEL",
			};
			if( ImGui.BeginCombo( "###Chat Channel Dropdown", currentChannelName ) )	//***** TODO: When chat rework happens, just use the localization functions from that (if that's how we do it) instead of switch above for currentChannelName and context menu options below.
			{
				if( ImGui.Selectable( GetNPCDialogueChannelName() ) )
				{
					mConfiguration.ChatChannelToUse = XivChatType.NPCDialogue;
				}
				if( ImGui.Selectable( GetNPCDialogueAnnouncementsChannelName() ) )
				{
					mConfiguration.ChatChannelToUse = XivChatType.NPCDialogueAnnouncements;
				}
				ImGui.EndCombo();
			}

			ImGui.Spacing();

			ImGui.Text( Loc.Localize( "Config Option: Minimum time between log messages.", "Minimum time between chat log messages:" ) );
			ImGuiUtils.HelpMarker( Loc.Localize( "Help: Minimum time between log messages.", "If multiple speech bubbles appear on-screen at the same time, this is how long will pass between each one going into the chat log." ) );
			ImGui.SliderInt( "###Minimum time between any log messages.", ref mConfiguration.mMinTimeBetweenChatPrints_mSec, 0, 2000, "%dms" );

			ImGui.Spacing();

			ImGui.Checkbox( Loc.Localize( "Config Option: Keep Line Breaks", "Keep line breaks." ) + "###Keep line breaks.", ref mConfiguration.mKeepLineBreaks );

			ImGui.Spacing();
			ImGui.Text( Loc.Localize( "Config Option: Default Speaker Name", "Default Speaker Name: " ) );
			ImGuiUtils.HelpMarker( Loc.Localize( "Help: Default Speaker Name", "The default name to show in the chat log for messages from NPCs that lack names." ) );
			ImGui.InputText( "###Default Speaker Name Input", ref mDefaultSenderNameConfigString, 20 );
			if( Plugin.IsValidSenderName( mDefaultSenderNameConfigString ) )
			{
				mConfiguration.DefaultSenderName = mDefaultSenderNameConfigString;
			}
			else
			{
				ImGui.PushStyleColor( ImGuiCol.Text, 0xee4444ff );
				ImGui.TextWrapped( Loc.Localize( "Error Message: Default Speaker Name", "The specified name is invalid; names must contain 20 total characters or fewer, and use only A-Z, a-z, ', -, and space." ) );
				ImGui.PopStyleColor();
			}

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

			if( ImGui.Button( Loc.Localize( "Button: Show Zone Overrides", "Manage Zone-Specific Settings" ) + "###ShowZoneOverridesButton" ) )
			{
				SettingsWindowZoneOverridesVisible = true;
			}

			ImGui.Spacing();
			ImGui.Spacing();
			ImGui.Spacing();
			ImGui.Spacing();
			ImGui.Spacing();

			if( ImGui.Button( Loc.Localize( "Button: Save", "Save" ) + "###Save Button" ) )
			{
				mConfiguration.Save();
			}
			ImGui.SameLine();
			if( ImGui.Button( Loc.Localize( "Button: Save and Close", "Save and Close" ) + "###Save and Close" ) )
			{
				mConfiguration.Save();
				SettingsWindowVisible = false;
			}
		}

		ImGui.End();
	}

	protected void DrawSettingsWindow_ZoneOverrides()
	{
		if( !SettingsWindowZoneOverridesVisible || !SettingsWindowVisible )
		{
			return;
		}

		if( ImGui.Begin( Loc.Localize( "Window Title: Config (Zone Overrides)", "\"Say What?\" Settings (Zone Overrides)" ) + "###\"Say What?\" Settings (Zone Overrides)",
			ref mSettingsWindowZoneOverridesVisible,
			ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse ) )
		{
			if( ImGui.Button( Loc.Localize( "Button: Add Zone Override", "Add/Select Current Zone" ) + "###ZoneOverrideAddButton" ) &&
				DalamudAPI.ClientState.TerritoryType > 0 )
			{
				if( !mConfiguration.mZoneConfigOverrideDict.ContainsKey( DalamudAPI.ClientState.TerritoryType ) )
				{
					mConfiguration.mZoneConfigOverrideDict.Add( DalamudAPI.ClientState.TerritoryType, new() );
				}

				mZoneOverrideSelectedTerritoryType = DalamudAPI.ClientState.TerritoryType;
			}
			if( ImGui.BeginChild( "###ZoneOverrideZoneListChild", new( 300, 200 ), true ) )
			{
				foreach( var item in mConfiguration.mZoneConfigOverrideDict )
				{
					string zoneName = mPlugin.GetNiceNameForZone( item.Key );
					
					if( ImGui.Selectable( zoneName, item.Key == mZoneOverrideSelectedTerritoryType ) )
					{
						mZoneOverrideSelectedTerritoryType = item.Key;
					}
				}
				ImGui.EndChild();
			}
			ImGui.Spacing();
			ImGui.Separator();
			ImGui.Spacing();
			if( mConfiguration.mZoneConfigOverrideDict.ContainsKey( mZoneOverrideSelectedTerritoryType ) )
			{
				var zoneConfig = mConfiguration.mZoneConfigOverrideDict[mZoneOverrideSelectedTerritoryType];

				if( zoneConfig != null )
				{
					ImGui.Text( Loc.Localize( "Config Label: Zone Override Options", "Options for the selected zone:" ) );
					ImGui.Checkbox( Loc.Localize( "Config Option: Disable for Zone.", "Ignore all NPC speech bubbles in this zone." ) + "###Disable for Zone Checkbox.", ref zoneConfig.mDisableForZone );
					if( !zoneConfig.DisableForZone )
					{
						ImGui.Checkbox( Loc.Localize( "Config Option: Allow repeated speech to print to log.", "Allow repeated speech to print to log." ) + "###Allow repeated speech to print to log (zone override).", ref zoneConfig.mRepeatsAllowed );
						ImGuiUtils.HelpMarker( Loc.Localize( "Help: Config Options Allow Repeated Speech", "If this is not checked, a given NPC speech bubble will never be repeated in the chat log until you change zones and come back." ) );
						if( zoneConfig.RepeatsAllowed )
						{
							ImGui.Text( Loc.Localize( "Config Option: Time before repeated speech can be printed again.", "Time before repeats are allowed:" ) );
							ImGui.SliderInt( "###Time before the same speech can be printed again (overworld).", ref zoneConfig.mTimeBeforeRepeatsAllowed_Sec, 1, 600, "%ds" );
						}
					}
				}
				else
				{
					ImGui.PushStyleColor( ImGuiCol.Text, 0xee4444ff );
					ImGui.Text( Loc.Localize( "Error Message: Zone Override Config Null", "The selected zone override data is corrupted, please delete this zone override and try again." ) );
					ImGui.PopStyleColor();
				}

				ImGui.PushStyleColor( ImGuiCol.Text, 0xee4444ff );
				if( ImGui.Button( Loc.Localize( "Button: Delete", "Delete" ) + "###ZoneOverrideDeleteButton" ) )
				{
					WantToDeleteSelectedZone = true;
				}
				if( WantToDeleteSelectedZone )
				{
					ImGui.Text( Loc.Localize( "Label: Confirm Delete Label", "Confirm delete: " ) );
					ImGui.SameLine();
					if( ImGui.Button( Loc.Localize( "Button: Yes", "Yes" ) + "###Yes Button" ) )
					{
						mConfiguration.mZoneConfigOverrideDict.Remove( mZoneOverrideSelectedTerritoryType );
						mZoneOverrideSelectedTerritoryType = 0;
						WantToDeleteSelectedZone = false;
					}
					ImGui.PushStyleColor( ImGuiCol.Text, 0xffffffff );
					ImGui.SameLine();
					if( ImGui.Button( Loc.Localize( "Button: No", "No" ) + "###No Button" ) )
					{
						WantToDeleteSelectedZone = false;
					}
					ImGui.PopStyleColor();
				}
				ImGui.PopStyleColor();
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
		return DalamudAPI.ClientState.ClientLanguage switch
		{
			Dalamud.Game.ClientLanguage.Japanese => "NPC会話",
			Dalamud.Game.ClientLanguage.English => "NPC Dialogue",
			Dalamud.Game.ClientLanguage.German => "NPC-Gespräche",
			Dalamud.Game.ClientLanguage.French => "Dialogues des PNJ",
			_ => "NPC Dialogue"
		};
	}

	protected string GetNPCDialogueAnnouncementsChannelName()
	{
		return DalamudAPI.ClientState.ClientLanguage switch
		{
			Dalamud.Game.ClientLanguage.Japanese => "NPC会話（アナウンス）",
			Dalamud.Game.ClientLanguage.English => "NPC Dialogue (Announcements)",
			Dalamud.Game.ClientLanguage.German => "Nachrichten von NPCs",
			Dalamud.Game.ClientLanguage.French => "Annonces des PNJ",
			_ => "NPC Dialogue (Announcements)"
		};
	}

	protected Plugin mPlugin;
	protected IDalamudPluginInterface mPluginInterface;
	protected Configuration mConfiguration;

	protected UInt32 mZoneOverrideSelectedTerritoryType = 0;
	public bool WantToDeleteSelectedZone { get; private set; } = false;

	protected string mDefaultSenderNameConfigString = "NPC";

	//	Need a real backing field on the following properties for use with ImGui.
	protected bool mSettingsWindowVisible = false;
	public bool SettingsWindowVisible
	{
		get { return mSettingsWindowVisible; }
		set { mSettingsWindowVisible = value; }
	}

	protected bool mSettingsWindowZoneOverridesVisible = false;
	public bool SettingsWindowZoneOverridesVisible
	{
		get { return mSettingsWindowZoneOverridesVisible; }
		set { mSettingsWindowZoneOverridesVisible = value; }
	}

	protected bool mDebugWindowVisible = false;
	public bool DebugWindowVisible
	{
		get { return mDebugWindowVisible; }
		set { mDebugWindowVisible = value; }
	}
}