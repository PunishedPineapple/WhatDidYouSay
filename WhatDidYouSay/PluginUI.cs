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

			if( ImGui.Begin( Loc.Localize( "Window Title: Config", "\"What did you say?\" Settings" ) + "###\"What did you say?\" Settings",
				ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse ) )
			{
				//***** TODO: Localization.
				ImGui.Text( "Minimum time between log messages (msec):" );
				ImGui.SliderInt( "###Minimum time between any log messages.", ref mConfiguration.mMinTimeBetweenChatPrints_mSec, 0, 2000 );

				ImGui.Spacing();
				ImGui.Spacing();
				ImGui.Spacing();
				ImGui.Spacing();
				ImGui.Spacing();

				ImGui.Text( "Overworld:" );
				ImGui.Indent();
				ImGui.Checkbox( "Allow repeated speech to print to log.", ref mConfiguration.mRepeatsAllowed );
				//***** TODO: Add note about always resetting when zoning.
				if( mConfiguration.RepeatsAllowed )
				{
					ImGui.Text( "Time before repeated speech can be printed again (sec):" );
					ImGui.SliderInt( "###Time before the same speech can be printed again.", ref mConfiguration.mTimeBeforeRepeatsAllowed_Sec, 1, 600 );
				}
				ImGui.Unindent();

				ImGui.Spacing();
				ImGui.Spacing();
				ImGui.Spacing();
				ImGui.Spacing();
				ImGui.Spacing();

				ImGui.Text( "Instance:" );
				ImGui.Indent();
				ImGui.Checkbox( "Allow repeated speech to print to log.###InInstance", ref mConfiguration.mRepeatsAllowedInInstance );
				//***** TODO: Add note about always resetting when zoning.
				if( mConfiguration.RepeatsAllowedInInstance )
				{
					ImGui.Text( "Time before repeated speech can be printed again (sec):" );
					ImGui.SliderInt( "###Time before the same speech can be printed again in instance.", ref mConfiguration.mTimeBeforeRepeatsAllowedInInstance_Sec, 1, 600 );
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
			if( ImGui.Begin( Loc.Localize( "Window Title: Debug Data", "\"What did you say?\" Debug Data" ) + "###\"What did you say?\" Debug Data", ref mDebugWindowVisible ) )
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