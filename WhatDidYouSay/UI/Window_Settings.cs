using System;

using CheapLoc;

using Dalamud.Bindings.ImGui;
using Dalamud.Game.Text;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;

namespace WhatDidYouSay;

internal sealed class Window_Settings : Window, IDisposable
{
	public Window_Settings( Plugin plugin, PluginUI pluginUI, Configuration configuration ) :
		base( Loc.Localize( "Window Title: Config", "\"Say What?\" Settings" ) + "###\"Say What?\" Settings" )
	{
		Flags = ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse;

		mPlugin = plugin;
		mPluginUI = pluginUI;
		mConfiguration = configuration;
	}

	public void Dispose()
	{
	}

	public override void OnOpen()
	{
		mDefaultSenderNameConfigString_Transient = mConfiguration.DefaultSenderName;
		base.OnOpen();
	}

	public override void OnClose()
	{
		mConfiguration.Save();
		base.OnClose();
	}

	public override void Draw()
	{
		ImGui.Text( Loc.Localize( "Config Option: Log message channel.", "Chat log channel for output:" ) );
		ImGuiUtils.HelpMarker( Loc.Localize( "Help: Log message channel.", "Make sure that the channel you select here is turned on in your character's log filter settings, or you won't see any messages." ) );

		string currentChannelName = mConfiguration.ChatChannelToUse switch
		{
			XivChatType.NPCDialogue => GetNPCDialogueChannelName(),
			XivChatType.NPCDialogueAnnouncements => GetNPCDialogueAnnouncementsChannelName(),
			_ => "INVALID CHANNEL",
		};

		if( ImGui.BeginCombo( "###Chat Channel Dropdown", currentChannelName ) )
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
		ImGui.SliderInt( "###Minimum time between any log messages.", ref mConfiguration.MinTimeBetweenChatPrints_mSec, 0, 2000, "%dms" );

		ImGui.Spacing();

		ImGui.Checkbox( Loc.Localize( "Config Option: Keep Line Breaks", "Keep line breaks." ) + "###Keep line breaks.", ref mConfiguration.KeepLineBreaks );

		ImGui.Spacing();
		ImGui.Checkbox( Loc.Localize( "Config Option: Use Curated NPC Names", "Use curated NPC names." ) + "###UseCuratedNPCNamesCheckbox", ref mConfiguration.UseCuratedNames );
		ImGui.Checkbox( Loc.Localize( "Config Option: Use Procedural NPC Names", "Use automatically-generated NPC names." ) + "###UseProceduralNPCNamesCheckbox", ref mConfiguration.UseProceduralNames );
		ImGui.Text( Loc.Localize( "Config Option: Default Speaker Name", "Default Speaker Name: " ) );
		ImGuiUtils.HelpMarker( Loc.Localize( "Help: Default Speaker Name", "The default name to show in the chat log for messages from NPCs that otherwise lack names." ) );
		ImGui.InputText( "###Default Speaker Name Input", ref mDefaultSenderNameConfigString_Transient, 20 );
		if( Plugin.IsValidCustomSenderName( mDefaultSenderNameConfigString_Transient ) )
		{
			mConfiguration.DefaultSenderName = mDefaultSenderNameConfigString_Transient;
		}
		else
		{
			ImGui.PushStyleColor( ImGuiCol.Text, 0xee4444ff );
			ImGui.TextWrapped( Loc.Localize( "Error Message: Default Speaker Name", "The specified name is invalid; names must contain 20 total characters or fewer, and use only A-Z, a-z, ', -, and space." ) );
			ImGui.PopStyleColor();
		}

		ImGui.Spacing();

		ImGui.Text( Loc.Localize( "Config Section: Overworld", "Overworld:" ) );
		ImGui.Indent();
		ImGui.Checkbox( Loc.Localize( "Config Option: Allow repeated speech to print to log.", "Allow repeated speech to print to log." ) + "###Allow repeated speech to print to log (overworld).", ref mConfiguration.RepeatsAllowed );
		ImGuiUtils.HelpMarker( Loc.Localize( "Help: Config Options Allow Repeated Speech", "If this is not checked, a given NPC speech bubble will never be repeated in the chat log until you change zones and come back." ) );
		if( mConfiguration.RepeatsAllowed )
		{
			ImGui.Text( Loc.Localize( "Config Option: Time before repeated speech can be printed again.", "Time before repeats are allowed:" ) );
			ImGui.SliderInt( "###Time before the same speech can be printed again (overworld).", ref mConfiguration.TimeBeforeRepeatsAllowed_Sec, 1, 600, "%ds" );
		}
		ImGui.Text( Loc.Localize( "Config Option: Minimum balloon lifetime.", "Minimum bubble lifetime:" ) );
		ImGui.SliderFloat( "###MinBalloonTime_Overworld", ref mConfiguration.MinBalloonTime_Sec, 0f, 15f, "%.1fs", ImGuiSliderFlags.AlwaysClamp );
		//ImGui.Checkbox( Loc.Localize( "Config Option: Repeat detection ignores sender ID.", "Treat identical messages from different entities as repeats." ) + "###Repeat detection ignores sender ID.", ref mConfiguration.RepeatDetectionIgnoresSenderID );
		ImGui.Unindent();

		ImGui.Spacing();

		ImGui.Text( Loc.Localize( "Config Section: Instance", "In Instance:" ) );
		ImGui.Indent();
		ImGui.Checkbox( Loc.Localize( "Config Option: Allow repeated speech to print to log.", "Allow repeated speech to print to log." ) + "###Allow repeated speech to print to log (InInstance)", ref mConfiguration.RepeatsAllowedInInstance );
		ImGuiUtils.HelpMarker( Loc.Localize( "Help: Config Options Allow Repeated Speech", "If this is not checked, a given NPC speech bubble will never be repeated in the chat log until you change zones and come back." ) );
		if( mConfiguration.RepeatsAllowedInInstance )
		{
			ImGui.Text( Loc.Localize( "Config Option: Time before repeated speech can be printed again.", "Time before repeats are allowed:" ) );
			ImGui.SliderInt( "###Time before the same speech can be printed again (in instance).", ref mConfiguration.TimeBeforeRepeatsAllowedInInstance_Sec, 1, 600, "%ds" );
		}
		ImGui.Text( Loc.Localize( "Config Option: Minimum balloon lifetime.", "Minimum bubble lifetime:" ) );
		ImGui.SliderFloat( "###MinBalloonTime_Instance", ref mConfiguration.MinBalloonTimeInInstance_Sec, 0f, 15f, "%.1fs", ImGuiSliderFlags.AlwaysClamp );
		//ImGui.Checkbox( Loc.Localize( "Config Option: Repeat detection ignores sender ID.", "Treat identical messages from different entities as repeats." ) + "###Repeat detection ignores sender ID.", ref mConfiguration.RepeatDetectionIgnoresSenderID_InInstance );
		ImGui.Unindent();

		ImGui.Spacing();

		if( ImGui.Button( Loc.Localize( "Button: Show Zone Overrides", "Manage Zone-Specific Settings" ) + "###ShowZoneOverridesButton" ) )
		{
			mPluginUI.Window_Settings_ZoneOverride.Toggle();
		}

		ImGuiHelpers.ScaledDummy( ImGuiUtils.SectionSpacingSize );

		if( ImGui.TreeNode( Loc.Localize( "Config Section: Advanced Options", "Advanced Options" ) + "###AdvancedOptionsHeader" ) )
		{
			ImGui.Text( Loc.Localize( "Config Option: Append NPC IDs to Messages", "Append IDs to messages for NPCs with:" ) );
			ImGui.Indent();
			ImGui.Checkbox( Loc.Localize( "Config Option: Append NPC IDs to Messages - Real Names", "Real Names" ) + "###AppendNPCIDsToMessagesCheckbox_RealNames", ref mConfiguration.ShowNPCIDInChatMessage_ForRealName );
			ImGui.Checkbox( Loc.Localize( "Config Option: Append NPC IDs to Messages - Curated Names", "Curated Names" ) + "###AppendNPCIDsToMessagesCheckbox_CuratedNames", ref mConfiguration.ShowNPCIDInChatMessage_ForCuratedName );
			ImGui.Checkbox( Loc.Localize( "Config Option: Append NPC IDs to Messages - Real Names", "Procedural Names" ) + "###AppendNPCIDsToMessagesCheckbox_ProceduralNames", ref mConfiguration.ShowNPCIDInChatMessage_ForProceduralName );
			ImGui.Checkbox( Loc.Localize( "Config Option: Append NPC IDs to Messages - Real Names", "No Names" ) + "###AppendNPCIDsToMessagesCheckbox_NoNames", ref mConfiguration.ShowNPCIDInChatMessage_ForNoName );
			ImGui.Unindent();
			ImGui.TreePop();
		}
	}

	//***** TODO: Recheck this since sheet things have changed.
	//	I wanted to get these from the sheets, but the relevant sheet (LogFilter) didn't seem to reliably load in time for some reason.
	private static string GetNPCDialogueChannelName()
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

	private static string GetNPCDialogueAnnouncementsChannelName()
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

	private readonly Plugin mPlugin;
	private readonly PluginUI mPluginUI;
	private readonly Configuration mConfiguration;

	//	Field to hold UI input for validation before committing to the plugin's config.
	private string mDefaultSenderNameConfigString_Transient = "NPC";
}
