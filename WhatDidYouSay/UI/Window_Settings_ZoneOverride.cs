using System;

using CheapLoc;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace WhatDidYouSay;

internal sealed class Window_Settings_ZoneOverride : Window, IDisposable
{
	public Window_Settings_ZoneOverride( Plugin plugin, PluginUI pluginUI, Configuration configuration ) :
		base( Loc.Localize( "Window Title: Config (Zone Overrides)", "\"Say What?\" Settings (Zone Overrides)" ) + "###\"Say What?\" Settings (Zone Overrides)" )
	{
		Flags = ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse;

		mPlugin = plugin;
		mPluginUI = pluginUI;
		mConfiguration = configuration;
	}

	public void Dispose()
	{
	}

	public override void Draw()
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
				string zoneName = ZoneSpecificConfig.GetNiceNameForZone( item.Key );

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

		if( mConfiguration.mZoneConfigOverrideDict.TryGetValue( mZoneOverrideSelectedTerritoryType, out var zoneConfig ) )
		{
			if( zoneConfig != null )
			{
				ImGui.Text( Loc.Localize( "Config Label: Zone Override Options", "Options for the selected zone:" ) );
				ImGui.Checkbox( Loc.Localize( "Config Option: Disable for Zone.", "Ignore all NPC speech bubbles in this zone." ) + "###Disable for Zone Checkbox.", ref zoneConfig.DisableForZone );
				if( !zoneConfig.DisableForZone )
				{
					ImGui.Checkbox( Loc.Localize( "Config Option: Allow repeated speech to print to log.", "Allow repeated speech to print to log." ) + "###Allow repeated speech to print to log (zone override).", ref zoneConfig.RepeatsAllowed );
					ImGuiUtils.HelpMarker( Loc.Localize( "Help: Config Options Allow Repeated Speech", "If this is not checked, a given NPC speech bubble will never be repeated in the chat log until you change zones and come back." ) );
					if( zoneConfig.RepeatsAllowed )
					{
						ImGui.Text( Loc.Localize( "Config Option: Time before repeated speech can be printed again.", "Time before repeats are allowed:" ) );
						ImGui.SliderInt( "###Time before the same speech can be printed again (overworld).", ref zoneConfig.TimeBeforeRepeatsAllowed_Sec, 1, 600, "%ds" );
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
				mWantToDeleteSelectedZone = true;
			}
			if( mWantToDeleteSelectedZone )
			{
				ImGui.Text( Loc.Localize( "Label: Confirm Delete Label", "Confirm delete: " ) );
				ImGui.SameLine();
				if( ImGui.Button( Loc.Localize( "Button: Yes", "Yes" ) + "###Yes Button" ) )
				{
					mConfiguration.mZoneConfigOverrideDict.Remove( mZoneOverrideSelectedTerritoryType );
					mZoneOverrideSelectedTerritoryType = 0;
					mWantToDeleteSelectedZone = false;
				}
				ImGui.PushStyleColor( ImGuiCol.Text, ImGui.GetStyle().Colors[(int)ImGuiCol.Text] );
				ImGui.SameLine();
				if( ImGui.Button( Loc.Localize( "Button: No", "No" ) + "###No Button" ) )
				{
					mWantToDeleteSelectedZone = false;
				}
				ImGui.PopStyleColor();
			}
			ImGui.PopStyleColor();
		}
	}

	private readonly Plugin mPlugin;
	private readonly PluginUI mPluginUI;
	private readonly Configuration mConfiguration;

	private UInt32 mZoneOverrideSelectedTerritoryType = 0;
	private bool mWantToDeleteSelectedZone = false;
}
