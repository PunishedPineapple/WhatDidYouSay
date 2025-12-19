using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

using CheapLoc;

using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Command;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Hooking;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalamud.Utility;

using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
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
				DalamudAPI.PluginLog.Information( $"AgentScreenLog::OpenBalloon function found at {addressInfo}." );
				mOpenBalloonHook = DalamudAPI.GameInteropProvider.HookFromAddress<OpenBalloonDelegate>( fpOpenBalloon, OpenBalloonDetour );
				mOpenBalloonHook?.Enable();
			}
			else
			{
				throw new Exception( "Unable to find the specified function signature for AgentScreenLog::OpenBalloon." );
			}

			/*IntPtr fpCloseBalloon = DalamudAPI.SigScanner.ScanText( "E8 ?? ?? ?? ?? EB ?? 45 33 C0 48 8D 4E" );
			if( fpCloseBalloon != IntPtr.Zero )
			{
				var addressInfo = Util.DescribeAddress( fpCloseBalloon );
				DalamudAPI.PluginLog.Information( $"AgentScreenLog::CloseBalloon function found at {addressInfo}." );
				mCloseBalloonHook = DalamudAPI.GameInteropProvider.HookFromAddress<CloseBalloonDelegate>( fpCloseBalloon, CloseBalloonDetour );
				mCloseBalloonHook?.Enable();
			}
			else
			{
				throw new Exception( "Unable to find the specified function signature for AgentScreenLog::CloseBalloon." );
			}*/
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
	}

	public void Dispose()
	{
		mOpenBalloonHook?.Disable();
		mOpenBalloonHook?.Dispose();

		//mCloseBalloonHook?.Disable();
		//mCloseBalloonHook?.Dispose();

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

	private unsafe void OpenBalloonDetour( AgentScreenLog* pAgentScreenLog, Character* pChara, byte* pString, bool param3, Int32 parentBoneId )
	{
		//	Do the bare minimum here that we need to track balloons later, both because it is safer to do things in our own framework
		//	event, and because it is cleaner to let the game complete what it is doing before we grab the relevant information.
		try
		{
			DalamudAPI.PluginLog.Verbose( $"OpenBalloon( 0x{(nint)pAgentScreenLog:X}, 0x{(nint)pChara:X}, 0x{(nint)pString:X}, {param3}, {parentBoneId} )" );

			if( pChara is null ) DalamudAPI.PluginLog.Warning( $"OpenBalloon called with a null Character*!  This should never happen." );
			else if( pChara->IsCharacter() )
			{
				var objectID = pChara->GetGameObjectId();
				mObjectsWithOpenedBalloons.Add( objectID );
				DalamudAPI.PluginLog.Debug( $"OpenBalloon called for character with object ID 0x{objectID:X} (at 0x{(nint)pChara:X}) on AgentScreenLog at 0x{(nint)pAgentScreenLog:X}." );
			}
			else DalamudAPI.PluginLog.Warning( $"OpenBalloon called with an object that is not a Character!  This should never happen." );
		}
		catch( Exception e )
		{
			DalamudAPI.PluginLog.Error( $"Exception in \"{nameof( OpenBalloonDetour )}\":\r\n{e}" );
		}
		finally
		{
			mOpenBalloonHook?.Original( pAgentScreenLog, pChara, pString, param3, parentBoneId );
		}
	}

	/*private unsafe void CloseBalloonDetour( AgentScreenLog* pAgentScreenLog, Character* pChara )
	{
		try
		{
			DalamudAPI.PluginLog.Verbose( $"CloseBalloon( 0x{(nint)pAgentScreenLog:X}, 0x{(nint)pChara:X} )" );

			if( pChara is null ) DalamudAPI.PluginLog.Information( $"CloseBalloon called with a null Character*!" );
			else if( pChara->IsCharacter() )
			{
				var objectID = pChara->GetGameObjectId();
				mObjectsWithClosedBalloons.Add( objectID );
				DalamudAPI.PluginLog.Debug( $"CloseBalloon called for character with object ID 0x{objectID:X} (at 0x{(nint)pChara:X}) on AgentScreenLog at 0x{(nint)pAgentScreenLog:X}." );
			}
			else DalamudAPI.PluginLog.Warning( $"CloseBalloon called with an object that is not a Character!  This should never happen." );
		}
		catch( Exception e )
		{
			DalamudAPI.PluginLog.Error( $"Exception in \"{nameof( CloseBalloonDetour )}\":\r\n{e}" );
		}
		finally
		{
			mCloseBalloonHook?.Original( pAgentScreenLog, pChara );
		}
	}*/

	private unsafe void OnGameFrameworkUpdate( IFramework framework )
	{
		if( !DalamudAPI.ClientState.IsLoggedIn ) return;

		//	Figure out how long we should track messages for repeats.
		TimeSpan messageInfoLifetime = TimeSpan.MaxValue;
		if( mConfiguration.mZoneConfigOverrideDict?.TryGetValue( DalamudAPI.ClientState.TerritoryType, out ZoneSpecificConfig? zoneConfig ) == true &&
			zoneConfig.RepeatsAllowed )
		{
			messageInfoLifetime = zoneConfig.TimeBeforeRepeatsAllowed;
		}
		else if( DalamudAPI.Condition[ConditionFlag.BoundByDuty] && mConfiguration.RepeatsAllowed )
		{
			messageInfoLifetime = mConfiguration.TimeBeforeRepeatsAllowed_InInstance;
		}
		else if( mConfiguration.RepeatsAllowed )
		{
			messageInfoLifetime = mConfiguration.TimeBeforeRepeatsAllowed;
		}
		
		//	Clean up any tracked message info that has expired.
		mNPCBalloonHistory.RemoveAll( x => x.IsExpired( messageInfoLifetime ) );

		//	Handle any new balloons.
		var objectsWithNewBalloons = DalamudAPI.ObjectTable.Where( x => mObjectsWithOpenedBalloons.Contains( x.GameObjectId ) );
		foreach( var obj in objectsWithNewBalloons )
		{
			if( obj.IsValid() && obj.Address != 0 && obj is ICharacter chara )
			{
				SeString? balloonText = null;
				var pChara = (Character*)chara.Address;
				if( !pChara->YellBalloon.Text.IsEmpty )
				{
					//***** TODO: Set the PlayTime for the balloon if below configured minimum.
					//***** TODO: If this is has the logmessage flag set, we're finished.
					balloonText = SeString.Parse( pChara->YellBalloon.Text );
					DalamudAPI.PluginLog.Verbose( $"Found valid text for NPCYellBalloon on object 0x{obj.GameObjectId:X}: \"{balloonText.TextValue}\"." );
				}
				else if( !pChara->Balloon.Text.IsEmpty )
				{
					//***** TODO: Set the PlayTime for the balloon if below configured minimum.
					balloonText = SeString.Parse( pChara->Balloon.Text );
					DalamudAPI.PluginLog.Verbose( $"Found valid text for Balloon on object 0x{obj.GameObjectId:X}: \"{balloonText.TextValue}\"." );
				}

				//	Create message info for this balloon if possible, and add or update as appropriate.
				if( balloonText is not null )
				{
					var balloonInfo = new NpcMessageInfo()
					{
						SpeakerObjectID = chara.GameObjectId,
						SpeakerKind = chara.ObjectKind,
						SpeakerBaseID = chara.BaseId,
						SpeakerNameID = chara.NameId,
						SpeakerName = chara.Name,
						Message = balloonText,
					};

					//***** TODO: Use the configured sender comparison type.
					var match = mNPCBalloonHistory.FirstOrDefault( x => x.Equals( balloonInfo, false, true, NpcMessageInfo.SenderComparisonType.ObjectID ) );
					if( match is not null )
					{
						match.FlagMessageSeenAgain();
						DalamudAPI.PluginLog.Verbose( $"Flagged existing message info for object 0x{obj.GameObjectId:X} as being seen again." );
					}
					else
					{
						mNPCBalloonHistory.Add( balloonInfo );
						DalamudAPI.PluginLog.Verbose( $"Added message info for Balloon on object 0x{obj.GameObjectId:X}." );
					}
				}
				else
				{
					DalamudAPI.PluginLog.Debug( $"Object with ID 0x{obj:X} opened a balloon, but neither known balloon structure contained valid text." );
				}
			}
		}

		//	Try to print any records that still need it.
		foreach( var balloonMessage in mNPCBalloonHistory.Where( x => !x.HasBeenPrinted ) )
		{
			balloonMessage.PrintToChatLog();
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
		ClearNPCBalloonHistory();
	}

	private void OnLogin()
	{
		UITextColorSelector.CacheUITheme();
	}

	private void OnLogout( int type, int code )
	{
		UITextColorSelector.CacheUITheme();
	}

	internal void ClearNPCBalloonHistory()
	{
		lock( mNPCBalloonHistoryLockObj ) mNPCBalloonHistory.Clear();
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
		lock( mNPCBalloonHistoryLockObj ) return mNPCBalloonHistory.ToList();
	}

	internal void DEBUG_InsertTestMessage( SeString sender, SeString message )
	{
		var balloonInfo = new NpcMessageInfo()
		{
			SpeakerObjectID = 0,
			SpeakerKind = Dalamud.Game.ClientState.Objects.Enums.ObjectKind.None,
			SpeakerBaseID = 0,
			SpeakerNameID = 0,
			SpeakerName = sender,
			Message = message,
		};

		lock( mNPCBalloonHistoryLockObj )
		{
			mNPCBalloonHistory.Add( balloonInfo );
		}
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

	private unsafe delegate void OpenBalloonDelegate( AgentScreenLog* pAgentScreenLog, Character* pChara, byte* pString, bool param3, Int32 parentBoneId );
	//private unsafe delegate void CloseBalloonDelegate( AgentScreenLog* pAgentScreenLog, Character* pChara );
	private readonly Hook<OpenBalloonDelegate>? mOpenBalloonHook = null;
	//private readonly Hook<CloseBalloonDelegate>? mCloseBalloonHook = null;

	private readonly Lock mNPCBalloonHistoryLockObj = new();
	private readonly List<NpcMessageInfo> mNPCBalloonHistory = new();

	private readonly HashSet<GameObjectId> mObjectsWithOpenedBalloons = new();
	//private readonly HashSet<GameObjectId> mObjectsWithClosedBalloons = new();

	private readonly IDalamudPluginInterface mPluginInterface;
}
