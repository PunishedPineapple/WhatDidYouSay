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
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
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
			mChatGui.ChatMessage += OnChatMessage;
		}

		//	Cleanup
		public void Dispose()
		{
			mOpenChatBubbleHook?.Disable();
			mOpenChatBubbleHook?.Dispose();

			mChatGui.ChatMessage -= OnChatMessage;
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

					SeString speakerName = SeString.Empty;
					if( pActor != null && pActor->GetName() != null )
					{
						speakerName = MemoryHelper.ReadSeStringNullTerminated( (IntPtr)pActor->GetName() );
					}
					var bubbleInfo = new SpeechBubbleInfo( MemoryHelper.ReadSeStringNullTerminated( pString ), currentTime_mSec, speakerName );

					lock( mSpeechBubbleInfoLockObj )
					{
						var extantMatch = mSpeechBubbleInfo.Find( ( x ) => { return x.IsSameMessageAs( bubbleInfo ); } );
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

		private void OnChatMessage( Dalamud.Game.Text.XivChatType type, UInt32 senderId, ref Dalamud.Game.Text.SeStringHandling.SeString sender, ref Dalamud.Game.Text.SeStringHandling.SeString message, ref bool isHandled )
		{
			//	We want to keep a short record of NPC dialogue messages that the game sends itself, because
			//	in some cases, these can duplicate speech bubbles.  We'll use these to avoid duplicate log lines.
			if( senderId != mOurFakeSenderID )
			{
				if( (UInt16)type == 0x3D && mConfiguration.IgnoreIfAlreadyInChat_NPCDialogue )  //***** TODO: Fix when enum updated in Dalamud.
				{
					lock( mGameChatInfoLockObj )
					{
						mGameChatInfo.Add( new( message, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), sender ) );
					}
					return;
				}

				if( (UInt16)type == 0x44 && mConfiguration.IgnoreIfAlreadyInChat_NPCDialogueAnnouncements ) //***** TODO: Fix when enum updated in Dalamud.
				{
					lock( mGameChatInfoLockObj )
					{
						mGameChatInfo.Add( new( message, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), sender ) );
					}
					return;
				}
			}
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
			}

			lock( mGameChatInfoLockObj )
			{
				//	Clean up any expired records.
				for( int i = mGameChatInfo.Count - 1; i >= 0; --i )
				{
					long timeSinceLastSeen_mSec = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - mGameChatInfo[i].TimeLastSeen_mSec;
					if( timeSinceLastSeen_mSec > 5000 )
					{
						mGameChatInfo.RemoveAt( i );
					}
				}
			}

			lock( mSpeechBubbleInfoLockObj )
			{
				//	Try to print any records that are new.
				for( int i = 0; i < mSpeechBubbleInfo.Count; ++i )
				{
					if( !mSpeechBubbleInfo[i].HasBeenPrinted )
					{
						SpeechBubbleInfo matchingChatEntry = null;
						lock( mGameChatInfoLockObj )
						{
							matchingChatEntry = mGameChatInfo.Find( ( x ) => { return x.IsSameMessageAs( mSpeechBubbleInfo[i] ); } );
						}
						if( matchingChatEntry != null )
						{
							mSpeechBubbleInfo[i].HasBeenPrinted = true;
						}
						else
						{
							mSpeechBubbleInfo[i].HasBeenPrinted = PrintChatMessage( mSpeechBubbleInfo[i].MessageText, mSpeechBubbleInfo[i].SpeakerName );
						}
					}
				}
			}
		}

		private bool PrintChatMessage( SeString msg, SeString speakerName )
		{
			//	Remove line breaks if desired.
			if( !mConfiguration.KeepLineBreaks )
			{
				msg = RemoveLineBreaks( msg );
			}

			//	Rate limit this as a last resort in case we messed up.
			long currentTime_mSec = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
			if( currentTime_mSec - mLastTimeChatPrinted_mSec >= mConfiguration.MinTimeBetweenChatPrints_mSec )
			{
				var chatEntry = new Dalamud.Game.Text.XivChatEntry
				{
					Type = mConfiguration.ChatChannelToUse,
					Name = speakerName,
					SenderId = mOurFakeSenderID,
					Message = msg
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

		private static SeString RemoveLineBreaks( SeString str )
		{
			SeString newStr = SeStringDeepCopy( str );

			for( int i = newStr.Payloads.Count - 1; i >= 0 ; --i )
			{
				if( newStr.Payloads[i].Type == PayloadType.NewLine )
				{
					if( i > 0 ) 
					{
						int j = i - 1;
						while( j >= 0 && newStr.Payloads[j].Type != PayloadType.RawText ) --j;

						if( j >= 0 )
						{
							TextPayload prevTextPayload = (TextPayload)newStr.Payloads[j];
							if( prevTextPayload.Text.EndsWith( '-' ) )
							{
								newStr.Payloads[j] = new TextPayload( prevTextPayload.Text[..^1] );
								newStr.Payloads.RemoveAt( i );
							}
							else if( !prevTextPayload.Text.EndsWith( ' ' ) )
							{
								newStr.Payloads[i] = new TextPayload( " " );
							}
						}
					}
					else
					{
						newStr.Payloads.RemoveAt( i );
					}
				}
			}

			return newStr;
		}

		unsafe private static SeString SeStringDeepCopy( SeString str )
		{
			var bytes = str.Encode();
			SeString newStr;
			fixed( byte* pBytes = bytes )
			{
				newStr = MemoryHelper.ReadSeStringNullTerminated( new IntPtr( pBytes ) );
			}
			newStr ??= SeString.Empty;
			return newStr;
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
			lock( mSpeechBubbleInfoLockObj )
			{
				return new( mSpeechBubbleInfo );
			}
		}

		internal List<SpeechBubbleInfo> GetGameChatInfo_DEBUG()
		{
			lock( mGameChatInfoLockObj )
			{
				return new( mGameChatInfo );
			}
		}

		internal void PrintChatMessage_DEBUG( SeString msg, SeString speakerName )
		{
			PrintChatMessage( msg, speakerName );
		}

		public string Name => "WhatDidYouSay";
		private const string mTextCommandName = "/saywhat";
		private const int mNumScreenTextBubbles = 10;
		private const UInt32 mOurFakeSenderID = 2;	//	Something unlikely to be any real sender ID so that we can quickly discriminate our own messages.

		private readonly PluginUI mUI;
		private readonly Configuration mConfiguration;

		private unsafe delegate IntPtr OpenChatBubbleDelegate( IntPtr pThis, GameObject* pActor, IntPtr pString, bool param3 );
		private readonly Hook<OpenChatBubbleDelegate> mOpenChatBubbleHook;

		private readonly Object mSpeechBubbleInfoLockObj = new();
		private readonly Object mGameChatInfoLockObj = new();
		private readonly List<SpeechBubbleInfo> mSpeechBubbleInfo = new();
		private readonly List<SpeechBubbleInfo> mGameChatInfo = new();
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
