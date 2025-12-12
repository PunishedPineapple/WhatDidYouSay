using System;
using System.Diagnostics;
using System.Text;

using CheapLoc;

using Dalamud.Bindings.ImGui;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using Dalamud.Memory;

using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Component.GUI;

using Lumina.Excel.Sheets;

using static Dalamud.Interface.Utility.Raii.ImRaii;

namespace WhatDidYouSay;

internal sealed class Window_Debug: Window, IDisposable
{
	public Window_Debug( Plugin plugin, PluginUI pluginUI, Configuration configuration ) :
		base( Loc.Localize( "Window Title: Debug Data", "\"Say What?\" Debug Data" ) + "###\"Say What?\" Debug Data" )
	{
		mPlugin = plugin;
		mPluginUI = pluginUI;
		mConfiguration = configuration;
	}

	public void Dispose()
	{
	}

	public unsafe override void Draw()
	{
		if( ImGui.Button( "Export Localizable Strings" ) )
		{
			mPlugin.ExportLocalizableStrings();
		}

		ImGuiHelpers.ScaledDummy( ImGuiUtils.SectionSpacingSize );

		ImGui.Checkbox( "Notify on non-zero param3", ref mConfiguration.DEBUG_NotifyOnUnknownParam3Bubble );

		ImGuiHelpers.ScaledDummy( ImGuiUtils.SectionSpacingSize );

		var pFramework = Framework.Instance();
		var pUIModule = pFramework != null ? pFramework->GetUIModule() : null;
		var pUI3DModule = pUIModule != null ? pUIModule->GetUI3DModule() : null;
		var pAgentModule = pUIModule != null ? pUIModule->GetAgentModule() : null;
		var pAgentScreenLog = pAgentModule != null ? pAgentModule->GetAgentScreenLog() : null;

		ImGuiUtils.CopyableAddress( "Framework", pFramework );
		ImGuiUtils.CopyableAddress( "UIModule", pUIModule );
		ImGuiUtils.CopyableAddress( "UI3DModule", pUI3DModule );
		ImGuiUtils.CopyableAddress( "AgentModule", pAgentModule );
		ImGuiUtils.CopyableAddress( "AgentScreenLog", pAgentScreenLog );

		ImGuiHelpers.ScaledDummy( ImGuiUtils.SectionSpacingSize );

		var pScreenLogAddon = (AtkUnitBase*)DalamudAPI.GameGui.GetAddonByName( "ScreenLog" ).Address;
		var atkValuesCount = pScreenLogAddon != null ? pScreenLogAddon->AtkValuesCount : 0;
		var pAtkValues = pScreenLogAddon != null ? pScreenLogAddon->AtkValues : null;
		ImGuiUtils.CopyableAddress( "ScreenLog addon", pScreenLogAddon );
		ImGuiUtils.CopyableAddress( $"AtkValues ({atkValuesCount})", pAtkValues );

		ImGuiHelpers.ScaledDummy( ImGuiUtils.SectionSpacingSize );

		ImGui.Checkbox( "Show all agent addresses", ref mShowAllAgentAddresses );

		if( mShowAllAgentAddresses )
		{
			if( ImGui.BeginChild( "###AgentInfoChildWindow", new( 500, 200 ), true, ImGuiWindowFlags.ChildWindow ) )
			{
				foreach( var pAgent in FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance()->GetUIModule()->GetAgentModule()->Agents )
				{
					if( pAgent.Value == null ) continue;
					ImGuiUtils.CopyableAddress( $"Agent Addon ID \"{pAgent.Value->AddonId}\"", pAgent.Value );
					//ImGui.Text( $"Agent Addon ID \"{pAgent.Value->AddonId}\" Address: 0x{(nint)pAgent.Value:X}" );
				}
			}

			ImGui.EndChild();
		}

		uint? channelColor = DalamudAPI.GameConfig.TryGet( Dalamud.Game.Config.UiConfigOption.ColorNpcSay, out uint color ) ? color : null;
		//if( channelColor.HasValue ) channelColor = channelColor.Value.ByteSwap();

		var UIColorSheet = DalamudAPI.DataManager.GetExcelSheet<UIColor>();
		uint? matchingUIColorIndex = null;

		foreach( var row in UIColorSheet )
		{
			var rowColor = UITextColorSelector.GetConfiguredUITheme() switch
			{
				1 => row.Light,
				2 => row.ClassicFF,
				3 => row.ClearBlue,
				_ => row.Dark,
			};

			if( ( rowColor & 0x00FFFFFF ) == channelColor )
			{
				matchingUIColorIndex = row.RowId;
				break;
			}
		}

		ImGuiHelpers.ScaledDummy( ImGuiUtils.SectionSpacingSize );

		ImGui.Text( $"NPC Dialog Channel Color: 0x{channelColor:X8}" );
		ImGui.Text( $"Found Matching Index: {matchingUIColorIndex}" );

		ImGuiHelpers.ScaledDummy( ImGuiUtils.SectionSpacingSize );

		ImGui.Text( "Print test message to chat log:" );
		SeString testSender = "Debug Sender";
		SeString testMessage = $"Message Color: { mConfiguration.NpcMessageTextColor}, { mConfiguration.NpcMessageGlowColor}.  Sender Color: { mConfiguration.SenderTextColor_Unknown}, { mConfiguration.SenderGlowColor_Unknown}.";
		ImGui.SameLine();
		if( ImGui.Button( "As Bubble" ) ) mPlugin.DEBUG_InsertTestMessageAsBubble( testSender, testMessage );
		ImGui.SameLine();
		if( ImGui.Button( "As Log Message" ) ) mPlugin.DEBUG_InsertTestMessageAsChat( testSender, testMessage );

		ImGuiHelpers.ScaledDummy( ImGuiUtils.SectionSpacingSize );

		if( ImGui.Button( "Reset seen speech history" ) )
		{
			mPlugin.ClearSpeechBubbleHistory();
		}

		ImGui.Spacing();

		foreach( var entry in mPlugin.DEBUG_GetSpeechBubbleInfo() )
		{
			if( ImGui.Button( $"P###btn-reprint-bubble{entry.GetHashCode()}" ) )
			{
				entry.PrintToChatLog();
			}
			ImGui.SameLine();
			ImGui.Text( $"String: {entry.Message}, Speaker: {entry.SpeakerName}, Time Last Seen: {entry.TimeLastSeen.ToString( "hh:mm:ss.fff" )}, Has Been Printed {entry.HasBeenPrinted}" );
			ImGui.Indent();
			foreach( var payload in entry.Message.Payloads )
			{
				ImGui.Text( $"Type: {payload.Type}, Contents: {payload.ToString()}" );
			}
			ImGui.Unindent();
		}

		ImGuiHelpers.ScaledDummy( ImGuiUtils.SectionSpacingSize );

		if( ImGui.Button( "Reset seen chat history" ) )
		{
			mPlugin.ClearGameChatHistory();
		}

		ImGui.Spacing();

		foreach( var entry in mPlugin.DEBUG_GetGameChatInfo() )
		{
			if( ImGui.Button( $"P###btn-reprint-chat{entry.GetHashCode()}" ) )
			{
				entry.PrintToChatLog();
			}
			ImGui.SameLine();
			ImGui.Text( $"String: {entry.Message}, Speaker: {entry.SpeakerName}, Time Last Seen: {entry.TimeLastSeen.ToString( "hh:mm:ss.fff" )}, Has Been Printed {entry.HasBeenPrinted}" );
		}
	}

	private bool mShowAllAgentAddresses = false;

	private readonly Plugin mPlugin;
	private readonly PluginUI mPluginUI;
	private readonly Configuration mConfiguration;
}
