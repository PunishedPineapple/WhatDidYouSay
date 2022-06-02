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
using Dalamud.Memory;
using Dalamud.Plugin;

using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;

namespace WhatDidYouSay
{
	public sealed class Plugin : IDalamudPlugin
	{
		//	Initialization
		public Plugin(
			DalamudPluginInterface pluginInterface,
			Dalamud.Game.Framework framework,
			SigScanner sigScanner,
			ClientState clientState,
			CommandManager commandManager,
			Condition condition,
			ChatGui chatGui )
		{
			//	API Access
			mPluginInterface	= pluginInterface;
			mFramework			= framework;
			mClientState		= clientState;
			mCommandManager		= commandManager;
			mCondition			= condition;
			mChatGui			= chatGui;

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

				IntPtr fpGetCIDForChatEntry = sigScanner.ScanText( "4C 8B 81 ?? ?? ?? ?? 4D 85 C0 74 17" );
				if( fpGetCIDForChatEntry != IntPtr.Zero )
				{
					PluginLog.LogInformation( $"GetCIDForChatEntry function signature found at 0x{fpGetCIDForChatEntry:X}." );
					GetCIDForChatEntryDelegate dGetCIDForChatEntry = GetCIDForChatEntryDetour;
					mGetCIDForChatEntryHook = new( fpGetCIDForChatEntry, dGetCIDForChatEntry );
					mGetCIDForChatEntryHook.Enable();
				}

				IntPtr fpSetCIDForChatEntry = sigScanner.ScanText( "4C 8B ?? 48 ?? ?? ?? ?? ?? ?? 48 85 ?? 74 ?? 41 ?? ?? ?? ?? ?? ?? 48" );
				if( fpSetCIDForChatEntry != IntPtr.Zero )
				{
					PluginLog.LogInformation( $"SetCIDForChatEntry function signature found at 0x{fpSetCIDForChatEntry:X}." );
					mdSetCIDForChatEntry = Marshal.GetDelegateForFunctionPointer<SetCIDForChatEntryDelegate>( fpSetCIDForChatEntry );
				}

				/*IntPtr fpSayChatCommand = sigScanner.ScanText( "?? 89 ?? ?? ?? 55 56 57 48 81 ?? ?? ?? ?? ?? 48 8B ?? ?? ?? ?? ?? 48 33 ?? ?? 89 ?? ?? ?? ?? ?? ?? 48 ?? ?? ?? 48 8B ?? 48 ?? ?? ?? 48 B8 ?? ?? ?? ?? ?? ?? ?? ?? 48 ?? ?? 49 8B ?? 49 8B ?? 48 C1 ?? ?? 48 8B ?? 48 C1 ?? ?? 48" );
				if( fpSayChatCommand != IntPtr.Zero )
				{
					PluginLog.LogInformation( $"SayChatCommand function signature found at 0x{fpSayChatCommand:X}." );
					SayChatCommandDelegate dSayChatCommand = SayChatCommandDetour;
					mSayChatCommandHook = new( fpSayChatCommand, dSayChatCommand );
					mSayChatCommandHook.Enable();
				}*/
			}

			//	UI Initialization
			mUI = new PluginUI( this, mConfiguration, pluginInterface, clientState );
			mPluginInterface.UiBuilder.Draw += DrawUI;
			mPluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;

			//	Event Subscription
			mPluginInterface.LanguageChanged += OnLanguageChanged;
			mClientState.TerritoryChanged += OnTerritoryChanged;
			mFramework.Update += OnGameFrameworkUpdate;
			mChatGui.ChatMessage += OnChatMessage;
			//mChatGui.CheckMessageHandled += OnCheckChatMessageHandled;
			//mChatGui.ChatMessageHandled += OnChatMessageHandled;
			//mChatGui.ChatMessageUnhandled += OnChatMessageUnhandled;
		}

		//	Cleanup
		public void Dispose()
		{
			mOpenChatBubbleHook?.Disable();
			mOpenChatBubbleHook?.Dispose();

			mGetCIDForChatEntryHook?.Disable();
			mGetCIDForChatEntryHook?.Dispose();

			mSayChatCommandHook?.Disable();
			mSayChatCommandHook?.Dispose();

			mChatGui.ChatMessage -= OnChatMessage;
			//mChatGui.CheckMessageHandled -= OnCheckChatMessageHandled;
			//mChatGui.ChatMessageHandled -= OnChatMessageHandled;
			//mChatGui.ChatMessageUnhandled -= OnChatMessageUnhandled;
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
				HelpMessage = Loc.Localize( "Plugin Text Command Description", "Opens the settings window." )
			} );
		}

		private void ProcessTextCommand( string command, string args )
		{
			if( args.ToLower() == "debug" )
			{
				mUI.DebugWindowVisible = !mUI.DebugWindowVisible;
			}
			else
			{
				mUI.SettingsWindowVisible = !mUI.SettingsWindowVisible;
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

		unsafe private ulong GetCIDForChatEntryDetour( IntPtr pThis, uint index )
		{
			ulong retval = mGetCIDForChatEntryHook.Original( pThis, index );
			PluginLog.LogDebug( $"Get CID for chat entry {index}: 0x{retval:X} (logptr: 0x{pThis:X})" );
			return retval;
		}

		/*unsafe private void SayChatCommandDetour( IntPtr pThis, IntPtr param2, IntPtr param3 )
		{
			try
			{
				PluginLog.LogDebug( $"Say chat detour: 0x{param2:X}, 0x{param3:X}" );
			}
			finally
			{
				mSayChatCommandHook.Original( pThis, param2, param3 );
			}
		}*/

		private void OnChatMessage( Dalamud.Game.Text.XivChatType type, UInt32 senderId, ref Dalamud.Game.Text.SeStringHandling.SeString sender, ref Dalamud.Game.Text.SeStringHandling.SeString message, ref bool isHandled )
		{
			//***** DEBUG
			if( type is Dalamud.Game.Text.XivChatType.Say or Dalamud.Game.Text.XivChatType.Shout or Dalamud.Game.Text.XivChatType.Yell )
			{
				string senderPayloadsStr = "";
				foreach( var payload in sender.Payloads )
				{
					senderPayloadsStr += $"{payload}, ";
				}
				var senderEncoded = sender.Encode();
				string encodedSenderStr = "";
				foreach( var character in senderEncoded )
				{
					encodedSenderStr += $"{character:X2} ";
				}
				PluginLog.LogDebug( $"OnMessage - Timestamp: {senderId}, Sender: {senderPayloadsStr}" );
				//if( type is Dalamud.Game.Text.XivChatType.Shout ) isHandled = true;
				lock( mGameChatInfoLockObj )
				{
					mGameChatInfo.Add( new( message, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), sender ) );
				}
			}

			//	We want to keep a short record of NPC dialogue messages that the game sends itself, because
			//	in some cases, these can duplicate speech bubbles.  We'll use these to avoid duplicate log lines.
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

		/*private void OnCheckChatMessageHandled( Dalamud.Game.Text.XivChatType type, UInt32 senderId, ref Dalamud.Game.Text.SeStringHandling.SeString sender, ref Dalamud.Game.Text.SeStringHandling.SeString message, ref bool isHandled )
		{
			if( type is Dalamud.Game.Text.XivChatType.Say or Dalamud.Game.Text.XivChatType.Shout or Dalamud.Game.Text.XivChatType.Yell )
			{
				PluginLog.LogDebug($"OnCheckHandled - Timestamp: {senderId}, Sender: {sender}, Message: {message}" );
			}
		}

		private void OnChatMessageHandled( Dalamud.Game.Text.XivChatType type, UInt32 senderId, Dalamud.Game.Text.SeStringHandling.SeString sender, Dalamud.Game.Text.SeStringHandling.SeString message )
		{
			if( type is Dalamud.Game.Text.XivChatType.Say or Dalamud.Game.Text.XivChatType.Shout or Dalamud.Game.Text.XivChatType.Yell )
			{
				PluginLog.LogDebug( $"OnHandled - Timestamp: {senderId}, Sender: {sender}, Message: {message}" );
			}
		}

		private void OnChatMessageUnhandled( Dalamud.Game.Text.XivChatType type, UInt32 senderId, Dalamud.Game.Text.SeStringHandling.SeString sender, Dalamud.Game.Text.SeStringHandling.SeString message )
		{
			if( type is Dalamud.Game.Text.XivChatType.Say or Dalamud.Game.Text.XivChatType.Shout or Dalamud.Game.Text.XivChatType.Yell )
			{
				PluginLog.LogDebug( $"OnUnhandled - Timestamp: {senderId}, Sender: {sender}, Message: {message}" );
			}
		}*/

		unsafe private void OnGameFrameworkUpdate( Dalamud.Game.Framework framework )
		{
			if( !mClientState.IsLoggedIn ) return;

			lock( mSpeechBubbleInfoLockObj )
			{
				//	Clean up any expired records.
				for( int i = mSpeechBubbleInfo.Count - 1; i >= 0; --i )
				{
					long timeSinceLastSeen_mSec = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - mSpeechBubbleInfo[i].TimeLastSeen_mSec;
					bool delete_OutOfInstance = mConfiguration.RepeatsAllowed && timeSinceLastSeen_mSec > Math.Max( 1, mConfiguration.TimeBeforeRepeatsAllowed_Sec ) * 1000;
					bool delete_InInstance = mConfiguration.RepeatsAllowedInInstance && timeSinceLastSeen_mSec > Math.Max( 1, mConfiguration.TimeBeforeRepeatsAllowedInInstance_Sec ) * 1000;
					if( mCondition[ConditionFlag.BoundByDuty] ? delete_InInstance : delete_OutOfInstance )
					{
						mSpeechBubbleInfo.RemoveAt( i );
					}
				}
			}

			if( !DisableGameChatDataClearing_DEBUG )
			{
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
			if( !DisableGameChatDataClearing_DEBUG ) ClearGameChatHistory();
		}

		internal void ClearSpeechBubbleHistory()
		{
			lock( mSpeechBubbleInfoLockObj )
			{
				mSpeechBubbleInfo.Clear();
			}
		}

		internal void ClearGameChatHistory()
		{
			lock( mGameChatInfoLockObj )
			{
				mGameChatInfo.Clear();
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

		internal void PrintTestChatMessage_DEBUG( SeString msg, SeString speakerName, Dalamud.Game.Text.XivChatType channel, IntPtr parameter )
		{
			var chatEntry = new Dalamud.Game.Text.XivChatEntry
			{
				Type = channel,
				Name = speakerName,
				//SenderId = 2442823091,
				//SenderId = (uint)(DateTimeOffset.UtcNow.ToUnixTimeSeconds() - 3600 * 6 + 500),//mOurFakeSenderID,
				Message = msg,
				Parameters = parameter,
			};

			mChatGui.PrintChat( chatEntry );
		}

		internal unsafe void AttachCIDToMessage( UInt64 contentID, UInt32 messageIndex, UInt16 serverID, UInt16 chatType )
		{
			
			mdSetCIDForChatEntry?.Invoke( new( FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance()->GetUiModule()->GetRaptureLogModule() ), contentID, messageIndex, serverID, chatType );
		}


		public string Name => "Say What?";
		private const string mTextCommandName = "/saywhat";

		private readonly PluginUI mUI;
		private readonly Configuration mConfiguration;

		private unsafe delegate IntPtr OpenChatBubbleDelegate( IntPtr pThis, GameObject* pActor, IntPtr pString, bool param3 );
		private readonly Hook<OpenChatBubbleDelegate> mOpenChatBubbleHook;

		private unsafe delegate ulong GetCIDForChatEntryDelegate( IntPtr pThis, uint index );
		private readonly Hook<GetCIDForChatEntryDelegate> mGetCIDForChatEntryHook;

		private unsafe delegate void SetCIDForChatEntryDelegate( IntPtr pThis, UInt64 contentID, UInt32 messageIndex, UInt16 serverID, UInt16 chatType );
		private readonly SetCIDForChatEntryDelegate mdSetCIDForChatEntry;
		//private readonly Hook<SetCIDForChatEntryDelegate> mSetCIDForChatEntryHook;

		private unsafe delegate void SayChatCommandDelegate( IntPtr pThis, IntPtr param2, IntPtr param3 );
		private readonly Hook<SayChatCommandDelegate> mSayChatCommandHook;

		private readonly Object mSpeechBubbleInfoLockObj = new();
		private readonly Object mGameChatInfoLockObj = new();
		private readonly List<SpeechBubbleInfo> mSpeechBubbleInfo = new();
		private readonly List<SpeechBubbleInfo> mGameChatInfo = new();
		private long mLastTimeChatPrinted_mSec;

		internal bool DisableGameChatDataClearing_DEBUG = true;

		private readonly DalamudPluginInterface mPluginInterface;
		private readonly Dalamud.Game.Framework mFramework;
		private readonly ClientState mClientState;
		private readonly CommandManager mCommandManager;
		private readonly Condition mCondition;
		private readonly ChatGui mChatGui;
	}
}
