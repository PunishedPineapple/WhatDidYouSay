using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

using CheapLoc;

using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.Command;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Hooking;
using Dalamud.Memory;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalamud.Utility;

using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;

namespace WhatDidYouSay;

public sealed class Plugin : IDalamudPlugin
{
	public Plugin( IDalamudPluginInterface pluginInterface )
	{
		//	API Access
		pluginInterface.Create<DalamudAPI>();
		mPluginInterface = pluginInterface;
		
		//	Configuration
		mConfiguration = mPluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
		mConfiguration.Initialize( mPluginInterface );
		if( !IsValidCustomSenderName( mConfiguration.DefaultSenderName ) ) mConfiguration.DefaultSenderName = "NPC";

		//	Other initialization.
		if( Instance is null )
		{
			Instance = this;
		}
		else
		{
			var errorStr =	"\"Plugin.Instance\" was not null!  This means that either another instance of the plugin is already running " +
							"in this context, or the last time the plugin was disabled, it was not properly disposed.";
			DalamudAPI.PluginLog.Error( errorStr );
			throw new( errorStr );
		}

		//	Localization and Command Initialization
		OnLanguageChanged( mPluginInterface.UiLanguage );

		//	Hook
		unsafe
		{
			IntPtr fpOpenBalloon = DalamudAPI.SigScanner.ScanText( "E8 ?? ?? ?? FF 48 8B 7C 24 48 C7 46 0C 01 00 00 00" );
			if( fpOpenBalloon != IntPtr.Zero )
			{
				var addressInfo = Util.DescribeAddress( fpOpenBalloon );
				DalamudAPI.PluginLog.Information( $"OpenChatBubble function found at {addressInfo}" );	//***** TODO: Verify and update other plugins to do it like this too.
				mOpenBalloonHook = DalamudAPI.GameInteropProvider.HookFromAddress<OpenBalloonDelegate>( fpOpenBalloon, OpenBalloonDetour );
				mOpenBalloonHook?.Enable();
			}
			else
			{
				throw new Exception( "Unable to find the specified function signature for OpenChatBubble." );
			}
		}

		//	UI Initialization
		mUI = new PluginUI( this, mConfiguration, pluginInterface );
		mPluginInterface.UiBuilder.Draw += DrawUI;
		mPluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUI;

		//	Event Subscription
		mPluginInterface.LanguageChanged += OnLanguageChanged;
		DalamudAPI.ClientState.Login += OnLogin;
		DalamudAPI.ClientState.Logout += OnLogout;
		DalamudAPI.ClientState.TerritoryChanged += OnTerritoryChanged;
		DalamudAPI.Framework.Update += OnGameFrameworkUpdate;
		DalamudAPI.ChatGui.ChatMessage += OnChatMessage;
	}

	public void Dispose()
	{
		mOpenBalloonHook?.Disable();
		mOpenBalloonHook?.Dispose();

		DalamudAPI.ChatGui.ChatMessage -= OnChatMessage;
		DalamudAPI.Framework.Update -= OnGameFrameworkUpdate;
		DalamudAPI.ClientState.TerritoryChanged -= OnTerritoryChanged;
		DalamudAPI.ClientState.Logout -= OnLogout;
		DalamudAPI.ClientState.Login -= OnLogin;
		mPluginInterface.LanguageChanged -= OnLanguageChanged;
		mPluginInterface.UiBuilder.Draw -= DrawUI;
		mPluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigUI;
		DalamudAPI.CommandManager.RemoveHandler( TextCommandName );

		mUI?.Dispose();

		Instance = null;
	}

	private void OnLanguageChanged( string langCode )
	{
		var allowedLang = new List<string>{ /*"es", "fr", "ja"*/ };

		DalamudAPI.PluginLog.Information( "Trying to set up Loc for culture {0}", langCode );

		if( allowedLang.Contains( langCode ) )
		{
			try
			{
				var locPath = Path.Combine( mPluginInterface.AssemblyLocation.DirectoryName, "Resources\\Localization\\", $"loc_{langCode}.json" );
				var locData = File.ReadAllText( locPath );
				Loc.Setup( locData );
			}
			catch( Exception e )
			{
				DalamudAPI.PluginLog.Error( $"Error in \"{nameof( OnLanguageChanged )}\".  Falling back to English.\r\n{e}" );
				Loc.SetupWithFallbacks();
			}
		}
		else
		{
			Loc.SetupWithFallbacks();
		}

		//	Set up the command handler with the current language.
		if( DalamudAPI.CommandManager.Commands.ContainsKey( TextCommandName ) )
		{
			DalamudAPI.CommandManager.RemoveHandler( TextCommandName );
		}
		DalamudAPI.CommandManager.AddHandler( TextCommandName, new CommandInfo( ProcessTextCommand )
		{
			HelpMessage = string.Format( Loc.Localize( "Plugin Text Command Description", "Opens the settings window.  Using the subcommands \"{0}\" or \"{1}\" will disable or reenable, respectively, the plugin for the current zone." ), SubcommandName_Ban, SubcommandName_Unban )
		} );
	}

	//***** TODO: Update to use newer command processing.
	private void ProcessTextCommand( string command, string args )
	{
		if( args.ToLower() == SubcommandName_Debug.ToLower() )
		{
			mUI.Window_Debug.Toggle();
		}
		else if( args.ToLower() == SubcommandName_Ban.ToLower() )
		{
			if( DalamudAPI.ClientState.TerritoryType > 0 )
			{
				if( !mConfiguration.mZoneConfigOverrideDict.ContainsKey( DalamudAPI.ClientState.TerritoryType ) )
				{
					mConfiguration.mZoneConfigOverrideDict.Add( DalamudAPI.ClientState.TerritoryType, new() );
				}

				if( mConfiguration.mZoneConfigOverrideDict.TryGetValue( DalamudAPI.ClientState.TerritoryType, out var zoneConfig ) )
				{
					zoneConfig.DisableForZone = true;
					DalamudAPI.ChatGui.Print( string.Format( Loc.Localize( "Subcommand Response: Current Zone Banned", "NPC speech bubbles will now be ignored in {0}." ), ZoneSpecificConfig.GetNiceNameForZone( DalamudAPI.ClientState.TerritoryType ) ) );
					mConfiguration.Save();
				}
				else
				{
					DalamudAPI.ChatGui.Print( string.Format( Loc.Localize( "Subcommand Response: Current Zone Banned (Error)", "Error: Unable to ban zone {0}." ), ZoneSpecificConfig.GetNiceNameForZone( DalamudAPI.ClientState.TerritoryType ) ) );
				}
			}
		}
		else if( args.ToLower() == SubcommandName_Unban.ToLower() )
		{
			if( mConfiguration.mZoneConfigOverrideDict.ContainsKey( DalamudAPI.ClientState.TerritoryType ) )
			{
				if( mConfiguration.mZoneConfigOverrideDict.Remove( DalamudAPI.ClientState.TerritoryType ) )
				{
					DalamudAPI.ChatGui.Print( string.Format( Loc.Localize( "Subcommand Response: Current Zone Unbanned", "NPC speech will now use your global settings in {0}." ), ZoneSpecificConfig.GetNiceNameForZone( DalamudAPI.ClientState.TerritoryType ) ) );
					mConfiguration.Save();
				}
				else
				{
					DalamudAPI.ChatGui.Print( string.Format( Loc.Localize( "Subcommand Response: Current Zone Unbanned (Error)", "Error: Unable to unban zone {0}." ), ZoneSpecificConfig.GetNiceNameForZone( DalamudAPI.ClientState.TerritoryType ) ) );
				}
			}
			else
			{
				//	Technically inaccurate, but effectively true from the user's perspective.
				DalamudAPI.ChatGui.Print( string.Format( Loc.Localize( "Subcommand Response: Current Zone Unbanned", "NPC speech will now use your global settings in {0}." ), ZoneSpecificConfig.GetNiceNameForZone( DalamudAPI.ClientState.TerritoryType ) ) );
			}
		}
		else
		{
			ToggleConfigUI();
		}
	}

	private void DrawUI() => mUI.Draw();

	private void ToggleConfigUI() => mUI.Window_Settings.Toggle();

	private unsafe void OpenBalloonDetour( AgentScreenLog* pThis, GameObject* pActor, byte* pString, byte param3, Int32 attachmentPoint )
	{
		try
		{
			//	First handle any logging that we want to do.
			LogBubbleInfo( pThis, pActor, pString, param3, attachmentPoint );

			ZoneSpecificConfig? zoneConfig = null;
			mConfiguration.mZoneConfigOverrideDict?.TryGetValue( DalamudAPI.ClientState.TerritoryType, out zoneConfig );

			//	The new player chat bubbles use a separate system, so we should theoretically not
			//	have to worry about them anymore, but have an option to keep checking in case some
			//	other plugin does something with them in the future.
			if( pActor is not null &&
				pString is not null &&
				!DalamudAPI.ClientState.IsPvPExcludingDen &&
				zoneConfig?.DisableForZone != true &&
				( pActor->ObjectKind != ObjectKind.Pc || !mConfiguration.IgnorePlayerCharacterBubbles ) )
			{
				var currentTime = DateTimeOffset.UtcNow;
				var bubbleInfo = new NpcMessageInfo()
				{
					SpeakerEntityID = pActor->EntityId,
					SpeakerKind = pActor->ObjectKind,
					SpeakerBaseID = pActor->ObjectKind is ObjectKind.BattleNpc or ObjectKind.EventNpc ? ((Character*)pActor)->BaseId : 0,
					SpeakerName = SeString.Parse( pActor->Name ) ?? SeString.Empty,
					Message = MemoryHelper.ReadSeStringNullTerminated( (nint)pString )
				};

				bool ignoreSenderIDForEquality = DalamudAPI.Condition[ConditionFlag.BoundByDuty] ?
				mConfiguration.RepeatDetectionIgnoresSenderID_InInstance :
				mConfiguration.RepeatDetectionIgnoresSenderID;

				lock( mSpeechBubbleInfoLockObj )
				{
					var extantMatch = mSpeechBubbleInfo.Find( x => x.Equals( bubbleInfo, ignoreSenderIDForEquality ) );
					if( extantMatch is not null ) extantMatch.FlagMessageSeenAgain();
					else mSpeechBubbleInfo.Add( bubbleInfo );
				}
			}
		}
		catch( Exception e )
		{
			DalamudAPI.PluginLog.Error( $"Exception in \"{nameof( OpenBalloonDetour )}\":\r\n{e}" );
		}
		finally
		{
			mOpenBalloonHook?.Original( pThis, pActor, pString, param3, attachmentPoint );
		}
	}

	private void OnChatMessage( XivChatType type, Int32 timestamp, ref SeString sender, ref SeString message, ref bool isHandled )
	{
		//***** TODO: Hook the function that determines whether to log the line and just skip if we're in that with a line that will print.

		//	We want to keep a short record of NPC dialogue messages that the game sends itself, because
		//	in some cases, these can duplicate speech bubbles.  We'll use these to avoid duplicate log lines.
		if( type.Channel == XivChatType.NPCDialogue && mConfiguration.IgnoreIfAlreadyInChat_NPCDialogue ||
			type.Channel == XivChatType.NPCDialogueAnnouncements && mConfiguration.IgnoreIfAlreadyInChat_NPCDialogueAnnouncements )
		{
			lock( mGameChatInfoLockObj )
			{
				mGameChatInfo.Add( new( 0xE000000, ObjectKind.None, 0, sender, message ) );
			}
		}
	}

	private unsafe void OnGameFrameworkUpdate( IFramework framework )
	{
		if( !DalamudAPI.ClientState.IsLoggedIn ) return;

		lock( mSpeechBubbleInfoLockObj )
		{
			//	Clean up any expired records.
			for( int i = mSpeechBubbleInfo.Count - 1; i >= 0; --i )
			{
				ZoneSpecificConfig? zoneConfig = null;
				mConfiguration.mZoneConfigOverrideDict?.TryGetValue( DalamudAPI.ClientState.TerritoryType, out zoneConfig );
				var timeSinceLastSeen = DateTimeOffset.UtcNow - mSpeechBubbleInfo[i].TimeLastSeen;
				bool delete_OutOfInstance = mConfiguration.RepeatsAllowed && timeSinceLastSeen > mConfiguration.TimeBeforeRepeatsAllowed;
				bool delete_InInstance = mConfiguration.RepeatsAllowedInInstance && timeSinceLastSeen > mConfiguration.TimeBeforeRepeatsAllowed_InInstance;
				
				if( zoneConfig is not null )
				{
					if( zoneConfig.RepeatsAllowed && timeSinceLastSeen > zoneConfig.TimeBeforeRepeatsAllowed )
					{
						mSpeechBubbleInfo.RemoveAt( i );
					}
				}
				else if( DalamudAPI.Condition[ConditionFlag.BoundByDuty] ? delete_InInstance : delete_OutOfInstance )
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
				var timeSinceLastSeen = DateTimeOffset.UtcNow - mGameChatInfo[i].TimeLastSeen;
				if( timeSinceLastSeen > TimeSpan.FromSeconds( 5 ) )
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
					NpcMessageInfo? matchingChatEntry = null;
					lock( mGameChatInfoLockObj )
					{
						matchingChatEntry = mGameChatInfo.Find( x => x.Equals( mSpeechBubbleInfo[i], true ) );
					}

					if( matchingChatEntry is not null ) mSpeechBubbleInfo[i].FlagGameHandledPrinting();
					else mSpeechBubbleInfo[i].PrintToChatLog();
				}
			}
		}
	}

	private unsafe void LogBubbleInfo( AgentScreenLog* pThis, GameObject* pActor, byte* pString, byte param3, Int32 attachmentPoint )
	{
		//	Verbose Logging
		if( DalamudAPI.PluginLog.MinimumLogLevel <= Serilog.Events.LogEventLevel.Verbose )
		{
			var actorName = pActor is not null ? SeString.Parse( pActor->Name )?.TextValue : "null speaker";
			if( string.IsNullOrWhiteSpace( actorName ) ) actorName = "empty speaker";
			var message = pString is not null ? MemoryHelper.ReadSeStringNullTerminated( (nint)pString )?.TextValue : "null message";
			if( string.IsNullOrWhiteSpace( message ) ) message = "empty message";
			string str = $"Call to OpenChatBubble( 0x{(nint)pThis:X}, 0x{(nint)pActor:X}, 0x{(nint)pString:X}, {param3}, {attachmentPoint} )\r\n";
			str += $"{actorName}: {message}\r\n";
			if( pActor is not null )
			{
				str += $"Kind - {pActor->ObjectKind}";
				str += $", EntityID - {pActor->EntityId}";
				str += $", BaseID - {pActor->BaseId}";
				//	90% sure that we could just GetNameId() it without checking, but I would rather not risk it.
				if( pActor->ObjectKind is ObjectKind.BattleNpc ) str += $", Name ID - {( (Character*)pActor )->NameId}";
			}
			DalamudAPI.PluginLog.Verbose( str );
		}

		//	Debug Logging
		if( param3 > 0 && DalamudAPI.PluginLog.MinimumLogLevel <= Serilog.Events.LogEventLevel.Debug )
		{
			string str = $"Atypical chat bubble opened in territory {DalamudAPI.ClientState?.TerritoryType ?? 0}:\r\n";
			str += $"AgentScreenLog*: 0x{(nint)pThis:X}\r\n";
			str += "Actor Info: ";
			if( pActor is null ) str += "Invalid!";
			else
			{
				str += $"Kind - {pActor->ObjectKind}";
				str += $", EntityID - {pActor->EntityId}";
				str += $", Name - \"{SeString.Parse( pActor->Name )?.TextValue ?? "Invalid!"}\"";
				str += $", BaseID - {pActor->BaseId}";
				//	90% sure that we could just GetNameId() it without checking, but I would rather not risk it.
				if( pActor->ObjectKind is ObjectKind.BattleNpc ) str += $", Name ID - {( (Character*)pActor )->NameId}";
			}
			str += $"\r\nparam3: {param3}";
			str += $"\r\nAttachment Point: {attachmentPoint}";

			DalamudAPI.PluginLog.Debug( str );

			if( mConfiguration.DEBUG_NotifyOnUnknownParam3Bubble )
			{
				SeString chatStr = new();
				chatStr.Payloads.Add( new UIForegroundPayload( 500 ) );
				chatStr.Payloads.Add( new TextPayload( "Atypical chat bubble encountered; check the log for more information." ) );
				chatStr.Payloads.Add( new UIForegroundPayload( 0 ) );

				DalamudAPI.ChatGui.Print( new()
				{
					Message = chatStr,
					Type = XivChatType.SystemMessage,
				} );
			}
		}
	}

	/*private UInt16 DetermineMessageTextColor()
	{
		//***** TODO: Find/add missing announcements color config if it exists.
		if( mConfiguration.NpcMessageTextColor > 0 )
		{
			return mConfiguration.NpcMessageTextColor;
		}
		else if( DalamudAPI.GameConfig.TryGet( Dalamud.Game.Config.UiConfigOption.ColorNpcSay, out uint color ) )
		{
			return (UInt16)color;
		}
		else
		{
			return 0;
		}
	}*/

	internal static bool IsValidCustomSenderName( SeString? str )
	{
		var textVal = str?.TextValue;
		if( textVal is null ) return false;

		return textVal.Length <= 20 && textVal.All( x => {
					return	x >= 'A' && x <= 'Z' ||
							x >= 'a' && x <= 'z' ||
							x is ' ' or '\'' or '-';
				} );
	}

	private void OnTerritoryChanged( UInt16 ID )
	{
		ClearSpeechBubbleHistory();
		ClearGameChatHistory();
	}

	private void OnLogin()
	{
		UITextColorSelector.CacheUITheme();
	}

	private void OnLogout( int type, int code )
	{
		UITextColorSelector.CacheUITheme();
	}

	internal void ClearSpeechBubbleHistory()
	{
		lock( mSpeechBubbleInfoLockObj ) mSpeechBubbleInfo.Clear();
	}

	internal void ClearGameChatHistory()
	{
		lock( mGameChatInfoLockObj ) mGameChatInfo.Clear();
	}

	internal void ExportLocalizableStrings()
	{
		string pwd = Directory.GetCurrentDirectory();

		try
		{
			var folderPath = mPluginInterface.IsDev ?
				mPluginInterface.AssemblyLocation.DirectoryName :
				mPluginInterface.GetPluginLocDirectory();

			if( !string.IsNullOrWhiteSpace( folderPath ) )
			{
				Directory.SetCurrentDirectory( folderPath );
				Loc.ExportLocalizable();
			}
		}
		finally
		{
			Directory.SetCurrentDirectory( pwd );
		}
	}

	internal IEnumerable<NpcMessageInfo> DEBUG_GetSpeechBubbleInfo()
	{
		lock( mSpeechBubbleInfoLockObj ) return mSpeechBubbleInfo.ToList();
	}

	internal IEnumerable<NpcMessageInfo> DEBUG_GetGameChatInfo()
	{
		lock( mGameChatInfoLockObj ) return mGameChatInfo.ToList();
	}

	internal void DEBUG_InsertTestMessageAsBubble( SeString sender, SeString message )
	{
		lock( mSpeechBubbleInfoLockObj ) mSpeechBubbleInfo.Add( new( 0xE0000000, ObjectKind.None, 0, sender, message ) );
	}

	internal void DEBUG_InsertTestMessageAsChat( SeString sender, SeString message )
	{
		lock( mSpeechBubbleInfoLockObj ) mGameChatInfo.Add( new( 0xE0000000, ObjectKind.None, 0, sender, message ) );
	}

	public static string Name => "Say What?";
	internal static string TextCommandName => "/saywhat";
	internal static string SubcommandName_Config => "config";
	internal static string SubcommandName_Ban => "ban";
	internal static string SubcommandName_Unban => "unban";
	internal static string SubcommandName_Debug => "debug";

	//***** TODO
	//	GameObjects can have names up to 64 bytes long, so we should also limit custom names to that length
	//	in case the log makes any assumptions.  For simplicity, just assume all characters are double-width.
	private const string ValidSenderNamePattern = @"\A[\p{L}\p{N}\p{P}\p{Zs}\p{Co}\●^`]{0,32}\z";

	private readonly PluginUI mUI;
	private readonly Configuration mConfiguration;

	//	Yes, this is a dumb thing to do.  There should never be multiple copies of the plugin (and if there
	//	are, all bets are already off for a lot of behavior), so the convenience outweighs the "smell".
	internal static Plugin? Instance { get; private set; } = null;
	internal static Configuration? CurrentConfiguration => Instance?.mConfiguration;

	//	Open bubble param3 probably corresponds to the "slowly" column in the Balloon exd, and param4 is bone
	//	index for attachment.  The actor is probably guaranteed to be a Character, but I am not 100% certain.
	//	Is the string pointer just a char*, or is it a Utf8String instance?  Does it matter?
	//***** TODO: Unlikely to be related, but check the fourth parameter when the message goes to both bubble and log.
	private unsafe delegate void OpenBalloonDelegate( AgentScreenLog* pThis, GameObject* pActor, byte* pString, byte param3, Int32 attachmentPoint );
	private readonly Hook<OpenBalloonDelegate>? mOpenBalloonHook = null;

	private readonly Lock mSpeechBubbleInfoLockObj = new();
	private readonly Lock mGameChatInfoLockObj = new();
	private readonly List<NpcMessageInfo> mSpeechBubbleInfo = new();
	private readonly List<NpcMessageInfo> mGameChatInfo = new();

	private readonly IDalamudPluginInterface mPluginInterface;
}
