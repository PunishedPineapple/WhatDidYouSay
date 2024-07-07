using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using CheapLoc;

using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.Command;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Hooking;
using Dalamud.Logging;
using Dalamud.Memory;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

using FFXIVClientStructs.FFXIV.Client.Game.Object;

using Lumina.Excel.GeneratedSheets;

using WhatDidYouSay.Services;

namespace WhatDidYouSay;

public sealed class Plugin : IDalamudPlugin
{
	//	Initialization
	public Plugin( IDalamudPluginInterface pluginInterface )
	{
		//	API Access
		pluginInterface.Create<Service>();
		mPluginInterface = pluginInterface;
		
		//	Configuration
		mConfiguration = mPluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
		mConfiguration.Initialize( mPluginInterface );
		if( !IsValidSenderName( mConfiguration.DefaultSenderName ) ) mConfiguration.DefaultSenderName = "NPC";

		//	Localization and Command Initialization
		OnLanguageChanged( mPluginInterface.UiLanguage );

		//	Hook
		unsafe
		{
			IntPtr fpOpenChatBubble = Service.SigScanner.ScanText( "E8 ?? ?? ?? FF 48 8B 7C 24 48 C7 46 0C 01 00 00 00" );
			if( fpOpenChatBubble != IntPtr.Zero )
			{
				Service.PluginLog.Information( $"OpenChatBubble function signature found at 0x{fpOpenChatBubble:X}." );
				mOpenChatBubbleHook = Service.GameInteropProvider.HookFromAddress<OpenChatBubbleDelegate>( fpOpenChatBubble, OpenChatBubbleDetour );
				mOpenChatBubbleHook?.Enable();
			}
			else
			{
				throw new Exception( "Unable to find the specified function signature for OpenChatBubble." );
			}
		}

		//	UI Initialization
		mUI = new PluginUI( this, mConfiguration, pluginInterface );
		mPluginInterface.UiBuilder.Draw += DrawUI;
		mPluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;

		//	Event Subscription
		mPluginInterface.LanguageChanged += OnLanguageChanged;
		Service.ClientState.TerritoryChanged += OnTerritoryChanged;
		Service.Framework.Update += OnGameFrameworkUpdate;
		Service.ChatGui.ChatMessage += OnChatMessage;
	}

	//	Cleanup
	public void Dispose()
	{
		mOpenChatBubbleHook?.Disable();
		mOpenChatBubbleHook?.Dispose();

		Service.ChatGui.ChatMessage -= OnChatMessage;
		Service.Framework.Update -= OnGameFrameworkUpdate;
		Service.ClientState.TerritoryChanged -= OnTerritoryChanged;
		mPluginInterface.LanguageChanged -= OnLanguageChanged;
		mPluginInterface.UiBuilder.Draw -= DrawUI;
		mPluginInterface.UiBuilder.OpenConfigUi -= DrawConfigUI;
		Service.CommandManager.RemoveHandler( TextCommandName );

		mUI?.Dispose();
	}

	private void OnLanguageChanged( string langCode )
	{
		var allowedLang = new List<string>{ /*"es", "fr", "ja"*/ };

		Service.PluginLog.Information( "Trying to set up Loc for culture {0}", langCode );

		if( allowedLang.Contains( langCode ) )
		{
			Loc.Setup( File.ReadAllText( Path.Join( Path.Join( mPluginInterface.AssemblyLocation.DirectoryName, "Resources\\Localization\\" ), $"loc_{langCode}.json" ) ) );
		}
		else
		{
			Loc.SetupWithFallbacks();
		}

		//	Set up the command handler with the current language.
		if( Service.CommandManager.Commands.ContainsKey( TextCommandName ) )
		{
			Service.CommandManager.RemoveHandler( TextCommandName );
		}
		Service.CommandManager.AddHandler( TextCommandName, new CommandInfo( ProcessTextCommand )
		{
			HelpMessage = String.Format( Loc.Localize( "Plugin Text Command Description", "Opens the settings window.  Using the subcommands \"{0}\" or \"{1}\" will disable or reenable, respectively, the plugin for the current zone." ), SubcommandName_Ban, SubcommandName_Unban )
		} );
	}

	private void ProcessTextCommand( string command, string args )
	{
		if( args.ToLower() == SubcommandName_Debug.ToLower() )
		{
			mUI.DebugWindowVisible = !mUI.DebugWindowVisible;
		}
		else if( args.ToLower() == SubcommandName_Ban.ToLower() )
		{
			if( Service.ClientState.TerritoryType > 0 )
			{
				if( !mConfiguration.mZoneConfigOverrideDict.ContainsKey( Service.ClientState.TerritoryType ) )
				{
					mConfiguration.mZoneConfigOverrideDict.Add( Service.ClientState.TerritoryType, new() );
				}

				if( mConfiguration.mZoneConfigOverrideDict.TryGetValue( Service.ClientState.TerritoryType, out var zoneConfig ) )
				{
					zoneConfig.DisableForZone = true;
					Service.ChatGui.Print( String.Format( Loc.Localize( "Subcommand Response: Current Zone Banned", "NPC speech bubbles will now be ignored in {0}." ), GetNiceNameForZone( Service.ClientState.TerritoryType ) ) );
					mConfiguration.Save();
				}
				else
				{
					Service.ChatGui.Print( String.Format( Loc.Localize( "Subcommand Response: Current Zone Banned (Error)", "Error: Unable to ban zone {0}." ), GetNiceNameForZone( Service.ClientState.TerritoryType ) ) );
				}
			}
		}
		else if( args.ToLower() == SubcommandName_Unban.ToLower() )
		{
			if( mConfiguration.mZoneConfigOverrideDict.ContainsKey( Service.ClientState.TerritoryType ) )
			{
				if( mConfiguration.mZoneConfigOverrideDict.Remove( Service.ClientState.TerritoryType ) )
				{
					Service.ChatGui.Print( String.Format( Loc.Localize( "Subcommand Response: Current Zone Unbanned", "NPC speech will now use your global settings in {0}." ), GetNiceNameForZone( Service.ClientState.TerritoryType ) ) );
					mConfiguration.Save();
				}
				else
				{
					Service.ChatGui.Print( String.Format( Loc.Localize( "Subcommand Response: Current Zone Unbanned (Error)", "Error: Unable to unban zone {0}." ), GetNiceNameForZone( Service.ClientState.TerritoryType ) ) );
				}
			}
			else
			{
				//	Technically inaccurate, but effectively true from the user's perspective.
				Service.ChatGui.Print( String.Format( Loc.Localize( "Subcommand Response: Current Zone Unbanned", "NPC speech will now use your global settings in {0}." ), GetNiceNameForZone( Service.ClientState.TerritoryType ) ) );
			}
		}
		else
		{
			mUI.SettingsWindowVisible = !mUI.SettingsWindowVisible;
		}
	}

	internal string GetNiceNameForZone( UInt32 territoryType )
	{
		var territoryTypeSheet = Service.DataManager.GetExcelSheet<TerritoryType>();
		var contentFinderConditionSheet = Service.DataManager.GetExcelSheet<ContentFinderCondition>();

		var territoryTypeForZone = territoryTypeSheet.GetRow( territoryType );
		var contentFinderConditionName = territoryTypeForZone?.ContentFinderCondition?.Value?.Name;

		if( contentFinderConditionName?.ToString().Trim().Length > 0 )
		{
			return contentFinderConditionName.ToString();
		}
		else if( territoryTypeForZone?.PlaceName.Value.Name.ToString().Trim().Length > 0 )
		{
			return territoryTypeForZone.PlaceName.Value.Name.ToString();
		}
		else
		{
			return $"{territoryType}";
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
		ZoneSpecificConfig zoneConfig = null;
		mConfiguration.mZoneConfigOverrideDict?.TryGetValue( Service.ClientState.TerritoryType, out zoneConfig );

		if( pString != IntPtr.Zero &&
			!Service.ClientState.IsPvPExcludingDen &&
			( zoneConfig == null || !zoneConfig.DisableForZone ) )
		{
			//	Idk if the actor can ever be null, but if it can, assume that we should print the bubble just in case.  Otherwise, only don't print if the actor is a player.
			if( pActor == null || (byte)pActor->ObjectKind != (byte)Dalamud.Game.ClientState.Objects.Enums.ObjectKind.Player )
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

	private void OnChatMessage( XivChatType type, Int32 timestamp, ref SeString sender, ref SeString message, ref bool isHandled )
	{
		//	We want to keep a short record of NPC dialogue messages that the game sends itself, because
		//	in some cases, these can duplicate speech bubbles.  We'll use these to avoid duplicate log lines.
		if( type == XivChatType.NPCDialogue && mConfiguration.IgnoreIfAlreadyInChat_NPCDialogue )
		{
			lock( mGameChatInfoLockObj )
			{
				mGameChatInfo.Add( new( message, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), sender ) );
			}
			return;
		}

		if( type == XivChatType.NPCDialogueAnnouncements && mConfiguration.IgnoreIfAlreadyInChat_NPCDialogueAnnouncements )
		{
			lock( mGameChatInfoLockObj )
			{
				mGameChatInfo.Add( new( message, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), sender ) );
			}
			return;
		}
	}

	unsafe private void OnGameFrameworkUpdate( IFramework framework )
	{
		if( !Service.ClientState.IsLoggedIn ) return;

		lock( mSpeechBubbleInfoLockObj )
		{
			//	Clean up any expired records.
			for( int i = mSpeechBubbleInfo.Count - 1; i >= 0; --i )
			{
				ZoneSpecificConfig zoneConfig = null;
				mConfiguration.mZoneConfigOverrideDict?.TryGetValue( Service.ClientState.TerritoryType, out zoneConfig );
				long timeSinceLastSeen_mSec = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - mSpeechBubbleInfo[i].TimeLastSeen_mSec;
				bool delete_OutOfInstance = mConfiguration.RepeatsAllowed && timeSinceLastSeen_mSec > Math.Max( 1, mConfiguration.TimeBeforeRepeatsAllowed_Sec ) * 1000;
				bool delete_InInstance = mConfiguration.RepeatsAllowedInInstance && timeSinceLastSeen_mSec > Math.Max( 1, mConfiguration.TimeBeforeRepeatsAllowedInInstance_Sec ) * 1000;
				
				if( zoneConfig != null )
				{
					if( zoneConfig.RepeatsAllowed && timeSinceLastSeen_mSec > Math.Max( 1, zoneConfig.TimeBeforeRepeatsAllowed_Sec ) * 1000 )
					{
						mSpeechBubbleInfo.RemoveAt( i );
					}
				}
				else if( Service.Condition[ConditionFlag.BoundByDuty] ? delete_InInstance : delete_OutOfInstance )
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
			if( speakerName.TextValue.Length == 0 &&
				mConfiguration.DefaultSenderName.Length > 0 &&
				IsValidSenderName( mConfiguration.DefaultSenderName ) )
			{
				speakerName = mConfiguration.DefaultSenderName;
			}

			var chatEntry = new Dalamud.Game.Text.XivChatEntry
			{
				Type = mConfiguration.ChatChannelToUse,
				Name = speakerName,
				Message = msg
			};

			Service.ChatGui.Print( chatEntry );
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

	internal static bool IsValidSenderName( string str )
	{
		return str.Length <= 20 && str.All( x => {
					return	( x >= 'A' && x <= 'Z' ) ||
							( x >= 'a' && x <= 'z' ) ||
							( x is ' ' or '\'' or '-' );
				} );
	}

	private void OnTerritoryChanged( UInt16 ID )
	{
		ClearSpeechBubbleHistory();
		ClearGameChatHistory();
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

	public string Name => "Say What?";
	internal static string TextCommandName => "/saywhat";
	internal static string SubcommandName_Config => "config";
	internal static string SubcommandName_Ban => "ban";
	internal static string SubcommandName_Unban => "unban";
	internal static string SubcommandName_Debug => "debug";

	//***** TODO: Maybe validate by regex, but can't figure out how to combine multiple unicode property escapes
	//private const string ValidSenderNamePattern = @"^[\p{L}'\- ]{0,20}$";

	private readonly PluginUI mUI;
	private readonly Configuration mConfiguration;

	private unsafe delegate IntPtr OpenChatBubbleDelegate( IntPtr pThis, GameObject* pActor, IntPtr pString, bool param3 );
	private readonly Hook<OpenChatBubbleDelegate> mOpenChatBubbleHook;

	private readonly Object mSpeechBubbleInfoLockObj = new();
	private readonly Object mGameChatInfoLockObj = new();
	private readonly List<SpeechBubbleInfo> mSpeechBubbleInfo = new();
	private readonly List<SpeechBubbleInfo> mGameChatInfo = new();
	private long mLastTimeChatPrinted_mSec;

	private readonly IDalamudPluginInterface mPluginInterface;
}
