using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

using CheapLoc;

using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.Hooking;
using Dalamud.Logging;
using Dalamud.Plugin;
using Dalamud.Memory;

using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.FFXIV.Client.Game.Object;

namespace WhatDidYouSay
{
	public sealed class Plugin : IDalamudPlugin
	{
		//	Initialization
		public Plugin(
			DalamudPluginInterface pluginInterface,
			Framework framework,
			SigScanner sigScanner,
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
			mConfiguration = mPluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
			mConfiguration.Initialize( mPluginInterface );

			//	Localization and Command Initialization
			OnLanguageChanged( mPluginInterface.UiLanguage );

			//	Hook
			unsafe
			{
				IntPtr fpOpenChatBubble = sigScanner.ScanText( "E8 ?? ?? ?? ?? F6 86 ?? ?? ?? ?? ?? C7 46 ?? ?? ?? ?? ??" );
				if( fpOpenChatBubble != IntPtr.Zero )
				{
					PluginLog.LogInformation( $"OpenChatBubble function signature found at 0x{fpOpenChatBubble:X}." );
					OpenChatBubbleDelegate dOpenChatBubble = OpenChatBubbleDetour;
					mOpenChatBubbleHook = new( fpOpenChatBubble, dOpenChatBubble );
					mOpenChatBubbleHook.Enable();
				}
				else
				{
					throw new Exception( "Unable to find the specified function signature for OpenChatBubble." );
				}
			}

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
			mOpenChatBubbleHook?.Disable();
			mOpenChatBubbleHook?.Dispose();

			mFramework.Update -= OnGameFrameworkUpdate;
			mClientState.TerritoryChanged -= OnTerritoryChanged;
			mPluginInterface.LanguageChanged -= OnLanguageChanged;
			mPluginInterface.UiBuilder.Draw -= DrawUI;
			mPluginInterface.UiBuilder.OpenConfigUi -= DrawConfigUI;
			mCommandManager.RemoveHandler( mTextCommandName );

			mUI?.Dispose();
		}

		private void OnLanguageChanged( string langCode )
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
				HelpMessage = String.Format( Loc.Localize( "Plugin Text Command Description", "Use \"{0}\" to open the the configuration window." ), mTextCommandName + " config" )
			} );
		}

		//	Text Commands
		private void ProcessTextCommand( string command, string args )
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

		private string ProcessTextCommand_Help( string args )
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

		private void DrawUI()
		{
			mUI.Draw();
		}

		private void DrawConfigUI()
		{
			mUI.SettingsWindowVisible = true;
		}

		unsafe private IntPtr OpenChatBubbleDetour( IntPtr pThis, GameObject* pActor, IntPtr pString, bool param3 )
		{
			if( pString != IntPtr.Zero && !mClientState.IsPvP )
			{
				//	Idk if the actor can ever be null, but if it can, assume that we should print the bubble just in case.  Otherwise, only don't print if the actor is a player.
				if( pActor == null || pActor->ObjectKind != (byte)Dalamud.Game.ClientState.Objects.Enums.ObjectKind.Player )
				{
					long currentTime_mSec = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
					var bubbleInfo = new SpeechBubbleInfo( Marshal.PtrToStringUTF8( pString ), currentTime_mSec );
					if( pActor != null && pActor->GetName() != null )
					{
						bubbleInfo.SpeakerName = MemoryHelper.ReadSeStringNullTerminated( (IntPtr)pActor->GetName() ).ToString();
					}

					lock( mSpeechBubbleInfoLockObj )
					{
						var extantMatch = mSpeechBubbleInfo.Find( ( x ) => { return x.MessageText.Equals( bubbleInfo.MessageText, StringComparison.InvariantCulture ); } );
						if( extantMatch != null )
						{
							extantMatch.TimeLastSeen_mSec = currentTime_mSec;
						}
						else
						{

							mSpeechBubbleInfo.Add( bubbleInfo );
						}
					}
				}
			}

			return mOpenChatBubbleHook.Original( pThis, pActor, pString, param3 );
		}

		unsafe private void OnGameFrameworkUpdate( Framework framework )
		{
			if( !mClientState.IsLoggedIn ) return;

			lock( mSpeechBubbleInfoLockObj )
			{
				//	Clean up any expired records.
				for( int i = mSpeechBubbleInfo.Count - 1; i >= 0; --i )
				{
					long timeSinceLastSeen_mSec = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - mSpeechBubbleInfo[i].TimeLastSeen_mSec;
					bool delete_OutOfInstance = mConfiguration.RepeatsAllowed && timeSinceLastSeen_mSec > mConfiguration.TimeBeforeRepeatsAllowed_Sec * 1000;
					bool delete_InInstance = mConfiguration.RepeatsAllowedInInstance && timeSinceLastSeen_mSec > mConfiguration.TimeBeforeRepeatsAllowedInInstance_Sec * 1000;
					if( mCondition[ConditionFlag.BoundByDuty] ? delete_InInstance : delete_OutOfInstance )
					{
						mSpeechBubbleInfo.RemoveAt( i );
					}
				}

				//	Try to print any records that are new.
				for( int i = 0; i < mSpeechBubbleInfo.Count; ++i )
				{
					if( !mSpeechBubbleInfo[i].HasBeenPrinted )
					{
						mSpeechBubbleInfo[i].HasBeenPrinted = PrintChatMessage( mSpeechBubbleInfo[i].MessageText, mSpeechBubbleInfo[i].SpeakerName );
					}
				}
			}
		}

		private bool PrintChatMessage( string msg, string speakerName )
		{
			//	Remove line breaks if desired.  SE uses a few different line breaks and control sequences for this it seems.
			if( !mConfiguration.KeepLineBreaks )
			{
				msg = msg.Replace( "\r\n", " " ).Replace( "\r", " " ).Replace( "\n", " " ).Replace( "\u0002\u0010\u0001\u0003", "" );
			}

			//	Rate limit this as a last resort in case we messed up.
			long currentTime_mSec = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
			if( currentTime_mSec - mLastTimeChatPrinted_mSec >= mConfiguration.MinTimeBetweenChatPrints_mSec )
			{
				var chatEntry = new Dalamud.Game.Text.XivChatEntry
				{
					Type = mConfiguration.ChatChannelToUse,
					Name = speakerName,
					Message = new Dalamud.Game.Text.SeStringHandling.SeString( new List<Dalamud.Game.Text.SeStringHandling.Payload>
				{
					new Dalamud.Game.Text.SeStringHandling.Payloads.TextPayload( msg )
				} )
				};

				mChatGui.PrintChat( chatEntry );
				mLastTimeChatPrinted_mSec = currentTime_mSec;
				return true;
			}
			else
			{
				return false;
			}
		}

		private void OnTerritoryChanged( object sender, UInt16 ID )
		{
			ClearSpeechBubbleHistory();
		}

		internal void ClearSpeechBubbleHistory()
		{
			lock( mSpeechBubbleInfoLockObj )
			{
				mSpeechBubbleInfo.Clear();
			}
		}

		internal List<SpeechBubbleInfo> GetSpeechBubbleInfo_DEBUG()
		{
			lock( mSpeechBubbleInfo )
			{
				return new( mSpeechBubbleInfo );
			}
		}

		public string Name => "WhatDidYouSay";
		private const string mTextCommandName = "/saywhat";
		private const int mNumScreenTextBubbles = 10;

		private readonly PluginUI mUI;
		private readonly Configuration mConfiguration;

		private unsafe delegate IntPtr OpenChatBubbleDelegate( IntPtr pThis, GameObject* pActor, IntPtr pString, bool param3 );
		private Hook<OpenChatBubbleDelegate> mOpenChatBubbleHook;

		private readonly Object mSpeechBubbleInfoLockObj = new();
		private readonly List<SpeechBubbleInfo> mSpeechBubbleInfo = new();
		private long mLastTimeChatPrinted_mSec;

		private readonly DalamudPluginInterface mPluginInterface;
		private readonly Framework mFramework;
		private readonly ClientState mClientState;
		private readonly CommandManager mCommandManager;
		private readonly Condition mCondition;
		private readonly ChatGui mChatGui;
		private readonly GameGui mGameGui;
	}
}
