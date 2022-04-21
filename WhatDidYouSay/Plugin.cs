using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.InteropServices;

using Dalamud.Plugin;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.Game;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Component.GUI;
using CheapLoc;

namespace WhatDidYouSay
{
	public class Plugin : IDalamudPlugin
	{
		//	Initialization
		public Plugin(
			DalamudPluginInterface pluginInterface,
			Framework framework,
			ClientState clientState,
			CommandManager commandManager,
			Condition condition,
			ChatGui chatGui,
			GameGui gameGui )
		{
			//	API Access
			mPluginInterface	= pluginInterface;
			mFramework			= framework;
			mClientState		= clientState;
			mCommandManager		= commandManager;
			mCondition			= condition;
			mChatGui			= chatGui;
			mGameGui			= gameGui;

			//	Configuration
			mPluginInterface = pluginInterface;
			mConfiguration = mPluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
			mConfiguration.Initialize( mPluginInterface );

			//	Localization and Command Initialization
			OnLanguageChanged( mPluginInterface.UiLanguage );

			//	UI Initialization
			mUI = new PluginUI( this, mConfiguration, mPluginInterface );
			mPluginInterface.UiBuilder.Draw += DrawUI;
			mPluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;
			mUI.Initialize();


			//	Event Subscription
			mPluginInterface.LanguageChanged += OnLanguageChanged;
			mClientState.TerritoryChanged += OnTerritoryChanged;
			mFramework.Update += OnGameFrameworkUpdate;
		}

		//	Cleanup
		public void Dispose()
		{
			mFramework.Update -= OnGameFrameworkUpdate;
			mClientState.TerritoryChanged -= OnTerritoryChanged;
			mPluginInterface.LanguageChanged -= OnLanguageChanged;
			mPluginInterface.UiBuilder.Draw -= DrawUI;
			mPluginInterface.UiBuilder.OpenConfigUi -= DrawConfigUI;
			mCommandManager.RemoveHandler( mTextCommandName );

			mUI.Dispose();
		}

		protected void OnLanguageChanged( string langCode )
		{
			var allowedLang = new List<string>{ /*"es", "fr", "ja"*/ };

			PluginLog.Information( "Trying to set up Loc for culture {0}", langCode );

			if( allowedLang.Contains( langCode ) )
			{
				Loc.Setup( File.ReadAllText( Path.Join( Path.Join( mPluginInterface.AssemblyLocation.DirectoryName, "Resources\\Localization\\" ), $"loc_{langCode}.json" ) ) );
			}
			else
			{
				Loc.SetupWithFallbacks();
			}

			//	Set up the command handler with the current language.
			if( mCommandManager.Commands.ContainsKey( mTextCommandName ) )
			{
				mCommandManager.RemoveHandler( mTextCommandName );
			}
			mCommandManager.AddHandler( mTextCommandName, new CommandInfo( ProcessTextCommand )
			{
				HelpMessage = String.Format( Loc.Localize( "Plugin Text Command Description", "Use {0} to open the the configuration window." ), "\"/whatdidyousay config\"" )
			} );
		}

		//	Text Commands
		protected void ProcessTextCommand( string command, string args )
		{
			//*****TODO: Don't split, just substring off of the first space so that other stuff is preserved verbatim.
			//	Seperate into sub-command and paramters.
			string subCommand = "";
			string subCommandArgs = "";
			string[] argsArray = args.Split( ' ' );
			if( argsArray.Length > 0 )
			{
				subCommand = argsArray[0];
			}
			if( argsArray.Length > 1 )
			{
				//	Recombine because there might be spaces in JSON or something, that would make splitting it bad.
				for( int i = 1; i < argsArray.Length; ++i )
				{
					subCommandArgs += argsArray[i] + ' ';
				}
				subCommandArgs = subCommandArgs.Trim();
			}

			//	Process the commands.
			bool suppressResponse = mConfiguration.SuppressCommandLineResponses;
			string commandResponse = "";
			if( subCommand.Length == 0 )
			{
				//	For now just have no subcommands act like the config subcommand
				mUI.SettingsWindowVisible = !mUI.SettingsWindowVisible;
			}
			else if( subCommand.ToLower() == "config" )
			{
				mUI.SettingsWindowVisible = !mUI.SettingsWindowVisible;
			}
			else if( subCommand.ToLower() == "debug" )
			{
				mUI.DebugWindowVisible = !mUI.DebugWindowVisible;
			}
			else if( subCommand.ToLower() == "help" || subCommand.ToLower() == "?" )
			{
				commandResponse = ProcessTextCommand_Help( subCommandArgs );
				suppressResponse = false;
			}
			else
			{
				commandResponse = ProcessTextCommand_Help( subCommandArgs );
			}

			//	Send any feedback to the user.
			if( commandResponse.Length > 0 && !suppressResponse )
			{
				mChatGui.Print( commandResponse );
			}
		}

		protected string ProcessTextCommand_Help( string args )
		{
			if( args.ToLower() == "config" )
			{
				return Loc.Localize( "Config Subcommand Help Message", "Opens the settings window." );
			}
			else if( args.ToLower() == "debug" )
			{
				return Loc.Localize( "Debug Subcommand Help Message", "Opens a debugging window." );
			}
			else
			{
				return String.Format( Loc.Localize( "Basic Help Message", "This plugin works automatically; however, some text commands are supported.  Valid subcommands are {0}.  Use \"{1} <subcommand>\" for more information on each subcommand." ), "\"config\"", mTextCommandName + " help" );
			}
		}

		protected void DrawUI()
		{
			mUI.Draw();
		}

		protected void DrawConfigUI()
		{
			mUI.SettingsWindowVisible = true;
		}

		unsafe protected void OnGameFrameworkUpdate( Framework framework )
		{
			if( !mClientState.IsLoggedIn ) return;

			var pMiniTalkAddon = (AtkUnitBase*)mGameGui.GetAddonByName( "_MiniTalk", 1 );
			if( pMiniTalkAddon != null )
			{
				bool addonVisible = pMiniTalkAddon->IsVisible;

				for( int i = 0; i < mNPCBubbleStrings.Length; ++i )
				{
					var pComponentNode = i+1 < pMiniTalkAddon->UldManager.NodeListCount ? (AtkComponentNode*)pMiniTalkAddon->UldManager.NodeList[i+1] : null;
					var pTextNode = pComponentNode != null && 3 < pComponentNode->Component->UldManager.NodeListCount ? pComponentNode->Component->UldManager.NodeList[3]->GetAsAtkTextNode() : null;

					bool bubbleVisible = false;
					if( pTextNode != null )
					{
						bubbleVisible = ((AtkResNode*)pComponentNode)->IsVisible && ((AtkResNode*)pTextNode)->IsVisible;
						if( bubbleVisible && !mPreviousNPCBubbleState[i] )
						{
							mNPCBubbleStrings_Temp[i] = pTextNode->NodeText.ToString();
							//mNPCBubbleStrings[i] = SeString.Parse( pTextNode->NodeText.StringPtr, (int)pTextNode->NodeText.StringLength );

							var chatEntry = new Dalamud.Game.Text.XivChatEntry
							{
								Type = (Dalamud.Game.Text.XivChatType)0x44,//Dalamud.Game.Text.XivChatType.NPCDialogueAnnouncements,
								Message = new Dalamud.Game.Text.SeStringHandling.SeString( new List<Dalamud.Game.Text.SeStringHandling.Payload>
								{
									new Dalamud.Game.Text.SeStringHandling.Payloads.TextPayload( mNPCBubbleStrings_Temp[i] )
								} )
							};
							mChatGui.PrintChat( chatEntry );
						}
					}

					mPreviousNPCBubbleState[i] = addonVisible && bubbleVisible;
				}
			}
			else
			{
				for( int i = 0; i < mPreviousNPCBubbleState.Length; ++i )
				{
					mPreviousNPCBubbleState[i] = false;
				}
			}
		}

		protected void OnTerritoryChanged( object sender, UInt16 ID )
		{
			//***** TODO: Clear the previously seen text lookup list
		}

		public string Name => "WhatDidYouSay";
		protected const string mTextCommandName = "/saywhat";
		protected PluginUI mUI;
		protected Configuration mConfiguration;

		protected const int mNumScreenTextBubbles = 10;
		public SeString[] mNPCBubbleStrings = new SeString[mNumScreenTextBubbles];
		public string[] mNPCBubbleStrings_Temp = new string[mNumScreenTextBubbles];
		public bool[] mPreviousNPCBubbleState = new bool[mNumScreenTextBubbles];

		protected DalamudPluginInterface mPluginInterface;
		protected Framework mFramework;
		protected ClientState mClientState;
		protected CommandManager mCommandManager;
		protected Condition mCondition;
		protected ChatGui mChatGui;
		protected GameGui mGameGui;
	}
}
