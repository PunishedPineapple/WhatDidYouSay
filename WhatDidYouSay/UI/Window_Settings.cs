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
		if( ImGui.BeginTabBar( "###SettingsWindowMainTabBar" ) )
		{
			if( ImGui.BeginTabItem( Loc.Localize( "Config Section: NPC Names", "NPC Names" ) + "###ConfigTab_NpcNames" ) )
			{
				ImGui.Checkbox( Loc.Localize( "Config Option: Use Curated NPC Names", "Use curated NPC names." ) + "###Checkbox_UseCuratedNpcNames", ref mConfiguration.UseCuratedNames );
				ImGuiUtils.HelpMarker( Loc.Localize( "Help: Use Curated NPC Names", "In cases where SE has left NPCs unnamed, assign community-created names to NPCs where available.  These attempt to emulate SE's naming style as much as possible for consistent flavor." ) );
				
				ImGui.Checkbox( Loc.Localize( "Config Option: Use Procedural NPC Names", "Use automatically-generated NPC names." ) + "###Checkbox_UseProceduralNpcNames", ref mConfiguration.UseProceduralNames );
				ImGuiUtils.HelpMarker( Loc.Localize( "Help: Use Procedural NPC Names", "In cases where SE has left NPCs unnamed (and no community name is available), assign names based upon the NPC model and the current region, such as \"Midlander Woman\"." ) );
				ImGui.Indent();
					if( !mConfiguration.UseProceduralNames ) ImGui.BeginDisabled();
					if( ImGui.RadioButton( Loc.Localize( "Config Option: Procedural NPC Names - Use Tribe", "Tribe" ) + "###RadioButton_ProceduralNpcNames_Tribe", mConfiguration.ProceduralNameRaceKind == RaceNameKind.Tribe ) ) mConfiguration.ProceduralNameRaceKind = RaceNameKind.Tribe;
					ImGui.SameLine();
					if( ImGui.RadioButton( Loc.Localize( "Config Option: Procedural NPC Names - Use Race", "Race" ) + "###RadioButton_ProceduralNpcNames_Race", mConfiguration.ProceduralNameRaceKind == RaceNameKind.Race ) ) mConfiguration.ProceduralNameRaceKind = RaceNameKind.Race;
					ImGui.Checkbox( Loc.Localize( "Config Option: Procedural NPC Names - Reduced Gender", "Reduced Gender" ) + "###Checkbox_ProceduralNpcNames_ReducedGender", ref mConfiguration.ProceduralNameReduceGender );
					ImGui.Checkbox( Loc.Localize( "Config Option: Procedural NPC Names - Ignore Region", "Ignore Region" ) + "###Checkbox_ProceduralNpcNames_IgnoreRegion", ref mConfiguration.ProceduralNameIgnoreRegion );
					if( NpcNameGenerator.TryGenerateSampleName( out var name, mConfiguration.ProceduralNameRaceKind, mConfiguration.ProceduralNameReduceGender, mConfiguration.ProceduralNameIgnoreRegion ) )
					{
						string exampleNameStr = string.Format(
							Loc.Localize( "Config String - Procedural Name Example", "Example: {0}" ),
							name );
						ImGui.Text( exampleNameStr );
					}
					if( !mConfiguration.UseProceduralNames ) ImGui.EndDisabled();
				ImGui.Unindent();
				ImGui.Text( Loc.Localize( "Config Option: Default Speaker Name", "Default Speaker Name: " ) );
				ImGuiUtils.HelpMarker( Loc.Localize( "Help: Default Speaker Name", "The default name to show in the chat log for messages from NPCs that otherwise lack names." ) );
				ImGui.InputText( "###InputText_DefaultSpeakerName", ref mDefaultSenderNameConfigString_Transient, 20 );
				if( Plugin.IsValidCustomSenderName( mDefaultSenderNameConfigString_Transient ) )
				{
					mConfiguration.DefaultSenderName = mDefaultSenderNameConfigString_Transient;
				}
				else
				{
					//***** TODO: Update the error message after changing the name's validation rules.
					ImGui.PushStyleColor( ImGuiCol.Text, 0xee4444ff );	//***** TODO: See if Dalamud has an error text color in the style instead of hard-coding red.
					ImGui.TextWrapped( Loc.Localize( "Error Message: Default Speaker Name", "The specified name is invalid; names must contain 20 total characters or fewer, and use only A-Z, a-z, ', -, and space." ) );
					ImGui.PopStyleColor();
				}
				ImGui.EndTabItem();
			}
			if( ImGui.BeginTabItem( Loc.Localize( "Config Section: Bubble Tweaks", "Bubble Tweaks" ) + "###ConfigTab_BubbleTweaks" ) )
			{
				ImGui.Text( Loc.Localize( "Config Option: Minimum Balloon Lifetime.", "Minimum Bubble Lifetime:" ) );
				ImGuiUtils.HelpMarker( Loc.Localize( "Help: Minimum Balloon Lifetime", "Extends NPC speech bubbles to be visible for at least this long.  However, if the game displays a new speech bubble on the NPC before this time expires, the new bubble always takes precedence." ) );
				ImGui.Indent();
					ImGui.Text( Loc.Localize( "Config Label: Overworld", "Overworld:" ) );
					ImGui.SameLine();
					ImGui.SliderFloat( "###Slider_MinBalloonTime_Overworld", ref mConfiguration.MinBalloonTime_Sec, 0f, 15f, "%.1f s", ImGuiSliderFlags.AlwaysClamp );
					ImGui.Text( Loc.Localize( "Config Label: In Instance", "In Instance:" ) );
					ImGui.SameLine();
					ImGui.SliderFloat( "###Slider_MinBalloonTime_Instance", ref mConfiguration.MinBalloonTimeInInstance_Sec, 0f, 15f, "%.1f s", ImGuiSliderFlags.AlwaysClamp );
				ImGui.Unindent();
				ImGui.EndTabItem();
			}
			if( ImGui.BeginTabItem( Loc.Localize( "Config Section: Chat Log", "Chat Log" ) + "###ConfigTab_ChatLog" ) )
			{
				ImGui.Checkbox( Loc.Localize( "Config Option: Keep Line Breaks", "Keep line breaks." ) + "###Checkbox_KeepLineBreaks.", ref mConfiguration.KeepLineBreaks );
				ImGui.Text( Loc.Localize( "Config Option: Log message channel.", "Chat log channel for output:" ) );
				ImGuiUtils.HelpMarker( Loc.Localize( "Help: Log message channel.", "Make sure that the channel you select here is turned on in your character's log filter settings, or you won't see any messages." ) );

				string currentChannelName = mConfiguration.ChatChannelToUse switch
				{
					XivChatType.NPCDialogue => GetNPCDialogueChannelName(),
					XivChatType.NPCDialogueAnnouncements => GetNPCDialogueAnnouncementsChannelName(),
					_ => "INVALID CHANNEL",
				};

				if( ImGui.BeginCombo( "###Dropdown_ChatChannel", currentChannelName ) )
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

				ImGui.Text( Loc.Localize( "Config Option: Minimum time between log messages.", "Minimum time between chat log messages:" ) );
				ImGuiUtils.HelpMarker( Loc.Localize( "Help: Minimum time between log messages.", "If multiple speech bubbles appear on-screen at the same time, this is how long will pass between each one going into the chat log." ) );
				ImGui.SliderInt( "###Slider_MinimumTimeBetweenLogMessages.", ref mConfiguration.MinTimeBetweenChatPrints_mSec, 0, 2000, "%d ms" );

				ImGui.EndTabItem();
			}
			if( ImGui.BeginTabItem( Loc.Localize( "Config Section: Reapeat Messages", "Repeat Messages" ) + "###ConfigTab_RepeatMessages" ) )
			{
				ImGui.Text( Loc.Localize( "Config Label: Overworld", "Overworld:" ) );
				ImGui.Indent();
				ImGui.Checkbox( Loc.Localize( "Config Option: Allow repeated speech to print to log", "Allow repeated speech to print to log" ) + "###Checkbox_AllowRepeats_Overworld", ref mConfiguration.RepeatsAllowed );
				ImGuiUtils.HelpMarker( Loc.Localize( "Help: Allow repeated speech to print to log", "If this is not checked, a given NPC speech bubble will never be repeated in the chat log until you change zones and come back." ) );
				if( !mConfiguration.RepeatsAllowed ) ImGui.BeginDisabled();
				{
					ImGui.Text( Loc.Localize( "Config Option: Time before repeated speech can be printed again.", "Time before repeats are allowed:" ) );
					ImGui.SliderInt( "###Slider_MinRepeatTime_Overworld", ref mConfiguration.TimeBeforeRepeatsAllowed_Sec, 1, 600, "%d s" );
				}
				ImGui.Checkbox( Loc.Localize( "Config Option: Repeat Detection Type", "Repeat detection is cumulative" ) + "###Checkbox_RepeatDetectionType_Overworld", ref mConfiguration.RepeatDetectionIsCumulative );
				ImGuiUtils.HelpMarker( string.Format( Loc.Localize( "Help: Repeat Detection Type", "If this is checked, a repeat within {0} seconds will cause repeats to be ignored for another {0} seconds, and so on." ), mConfiguration.TimeBeforeRepeatsAllowed_Sec ) );
				ImGui.Text( Loc.Localize( "Config Option: Repeat Sender Comparison Type", "Sender Comparison Type:" ) );
				ImGuiUtils.HelpMarker( Loc.Localize( "Help: Repeat Sender Comparison Type", "When checking for repeat NPC bubbles, controls how senders are compared.  This can be used to treat multiple NPCs that have identical names and/or models as the same NPC to help prevent chat log spam." ) );
				ImGui.Text( Loc.Localize( "Config Label: ENpcs", "Non-Combatant NPCs" ) );
				ImGui.SameLine();
				var senderComparisonEnumVals = (SenderComparisonType[])Enum.GetValuesAsUnderlyingType<SenderComparisonType>();
				ImGui.Combo<SenderComparisonType>( "###Combo_RepeatSenderComparisonType_ENpc",
					ref mConfiguration.mRepeatDetectionSenderComparisonType_ENpc,
					senderComparisonEnumVals,
					x => x.GetTranslatedName() );
				ImGui.Text( Loc.Localize( "Config Label: BNpcs", "Combatant NPCs" ) );
				ImGui.SameLine();
				ImGui.Combo<SenderComparisonType>( "###Combo_RepeatSenderComparisonType_BNpc",
					ref mConfiguration.mRepeatDetectionSenderComparisonType_BNpc,
					senderComparisonEnumVals,
					x => x.GetTranslatedName() );
				if( !mConfiguration.RepeatsAllowed ) ImGui.EndDisabled();
				ImGui.Unindent();

				ImGuiHelpers.ScaledDummy( ImGuiUtils.SectionSpacingSize );

				ImGui.Text( Loc.Localize( "Config Label: Instance", "In Instance:" ) );
				ImGui.Indent();
				ImGui.Checkbox( Loc.Localize( "Config Option: Allow repeated speech to print to log", "Allow repeated speech to print to log" ) + "###Checkbox_AllowRepeats_InInstance", ref mConfiguration.RepeatsAllowedInInstance );
				ImGuiUtils.HelpMarker( Loc.Localize( "Help: Allow repeated speech to print to log", "If this is not checked, a given NPC speech bubble will never be repeated in the chat log until you change zones and come back." ) );
				if( !mConfiguration.RepeatsAllowedInInstance ) ImGui.BeginDisabled();
				{
					ImGui.Text( Loc.Localize( "Config Option: Time before repeated speech can be printed again.", "Time before repeats are allowed:" ) );
					ImGui.SliderInt( "###Slider_MinRepeatTime_InInstance", ref mConfiguration.TimeBeforeRepeatsAllowedInInstance_Sec, 1, 600, "%d s" );
				}
				ImGui.Checkbox( Loc.Localize( "Config Option: Repeat Detection Type", "Repeat detection is cumulative" ) + "###Checkbox_RepeatDetectionType_InInstance", ref mConfiguration.RepeatDetectionIsCumulative_InInstance );
				ImGuiUtils.HelpMarker( string.Format( Loc.Localize( "Help: Repeat Detection Type", "If this is checked, a repeat within {0} seconds will cause repeats to be ignored for another {0} seconds, and so on." ), mConfiguration.TimeBeforeRepeatsAllowedInInstance_Sec ) );
				ImGui.Text( Loc.Localize( "Config Option: Repeat Sender Comparison Type", "Sender Comparison Type:" ) );
				ImGuiUtils.HelpMarker( Loc.Localize( "Help: Repeat Sender Comparison Type", "When checking for repeat NPC bubbles, controls how senders are compared.  This can be used to treat multiple NPCs that have identical names and/or models as the same NPC to help prevent chat log spam." ) );
				ImGui.Text( Loc.Localize( "Config Label: ENpcs", "Non-Combatant NPCs" ) );
				ImGui.SameLine();
				ImGui.Combo<SenderComparisonType>( "###Combo_RepeatSenderComparisonType_ENpc_InInstance",
					ref mConfiguration.mRepeatDetectionSenderComparisonType_ENpc_InInstance,
					senderComparisonEnumVals,
					x => x.GetTranslatedName() );
				ImGui.Text( Loc.Localize( "Config Label: BNpcs", "Combatant NPCs" ) );
				ImGui.SameLine();
				ImGui.Combo<SenderComparisonType>( "###Combo_RepeatSenderComparisonType_BNpc_InInstance",
					ref mConfiguration.mRepeatDetectionSenderComparisonType_BNpc,
					senderComparisonEnumVals,
					x => x.GetTranslatedName() );
				if( !mConfiguration.RepeatsAllowedInInstance ) ImGui.EndDisabled();
				ImGui.Unindent();
				ImGui.EndTabItem();
			}
			if( ImGui.BeginTabItem( Loc.Localize( "Config Section: Zone-Specific Settings", "Zone-Specific Settings" ) + "###ConfigTab_ZoneSpecificSettings" ) )
			{
				//***** TODO: Move out of own window into this tab.
				if( ImGui.Button( Loc.Localize( "Button: Show Zone Overrides", "Manage Zone-Specific Settings" ) + "###ShowZoneOverridesButton" ) )
				{
					mPluginUI.Window_Settings_ZoneOverride.Toggle();
				}
				ImGui.EndTabItem();
			}
			if( ImGui.BeginTabItem( Loc.Localize( "Config Section: Developer Options", "Developer Options" ) + "###ConfigTab_DeveloperOptions" ) )
			{
				ImGui.Text( Loc.Localize( "Config Section: Append NPC IDs to Messages", "Append IDs to messages for NPCs with:" ) );
				ImGuiUtils.HelpMarker( Loc.Localize( "Help: Append NPC IDs to Messages", "Primarily for use in identifying NPCs to add to the list of curated names." ) );
				ImGui.Indent();
				ImGui.Checkbox( Loc.Localize( "Config Option: Append NPC IDs to Messages - Real Names", "Real Names" ) + "###Checkbox_AppendNpcIdsToMessages_RealNames", ref mConfiguration.ShowNpcIdInChatMessage_ForRealName );
				ImGui.Checkbox( Loc.Localize( "Config Option: Append NPC IDs to Messages - Curated Names", "Curated Names" ) + "###Checkbox_AppendNpcIdsToMessagesCheckbox_CuratedNames", ref mConfiguration.ShowNpcIdInChatMessage_ForCuratedName );
				ImGui.Checkbox( Loc.Localize( "Config Option: Append NPC IDs to Messages - Procedural Names", "Procedural Names" ) + "###Checkbox_AppendNpcIdsToMessagesCheckbox_ProceduralNames", ref mConfiguration.ShowNpcIdInChatMessage_ForProceduralName );
				ImGui.Checkbox( Loc.Localize( "Config Option: Append NPC IDs to Messages - No Names", "No Names" ) + "###Checkbox_AppendNpcIdsToMessages_NoNames", ref mConfiguration.ShowNpcIdInChatMessage_ForNoName );
				ImGui.Unindent();

				ImGui.EndTabItem();
			}
			ImGui.EndTabBar();
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
