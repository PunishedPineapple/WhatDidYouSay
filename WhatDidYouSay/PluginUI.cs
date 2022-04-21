using System;
using System.Collections.Generic;
using System.Numerics;
using System.Linq;
using System.Globalization;
using System.IO;

using ImGuiNET;
using ImGuiScene;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using Dalamud.Plugin;
using Dalamud.Data;
using Dalamud.Game.Gui;
using Dalamud.Interface;
using Dalamud.Game;
using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Component.GUI;
using CheapLoc;

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
				ImGui.Text( Loc.Localize( "Config Option: Minimum time between log messages.", "Minimum time between log messages (msec):" ) );
				ImGui.SliderInt( "###Minimum time between any log messages.", ref mConfiguration.mMinTimeBetweenChatPrints_mSec, 0, 2000 );

				ImGui.Spacing();
				ImGui.Spacing();
				ImGui.Spacing();
				ImGui.Spacing();
				ImGui.Spacing();

				//***** TODO: Revisit this when Dalamud has the right chat type enums.
				ImGui.Text( Loc.Localize( "Config Option: Log message channel.", "Log channel to use:" ) );
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

				ImGui.Text( "Overworld:" );
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

				ImGui.Text( "Instance:" );
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
					ImGui.Text( $"String: {entry.MessageText}, Time Last Seen: {entry.TimeLastSeen_mSec}, Has Been Printed {entry.HasBeenPrinted}" );
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