using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

using CheapLoc;

using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Command;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Hooking;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalamud.Utility;

using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;

namespace WhatDidYouSay;

public sealed class Plugin : IDalamudPlugin
{
	public Plugin( IDalamudPluginInterface pluginInterface )
	{
		//	API Access
		pluginInterface.Create<DalamudAPI>();
		mPluginInterface = pluginInterface;

		//***** TODO: Custom ClientStructs; remove before releasing plugin.
		InteropGenerator.Runtime.Resolver.GetInstance.Setup(
			DalamudAPI.SigScanner.SearchBase,
			DalamudAPI.DataManager.GameData.Repositories["ffxiv"].Version,
			new FileInfo( Path.Join( pluginInterface.ConfigDirectory.FullName, "SigCache.json" ) ) );
		//InteropGenerator.Runtime.Resolver.GetInstance.Setup();
		FFXIVClientStructs.Interop.Generated.Addresses.Register();
		InteropGenerator.Runtime.Resolver.GetInstance.Resolve();

		//	Configuration
		mConfiguration = mPluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
		mConfiguration.Initialize( mPluginInterface );
		if( !IsValidCustomSenderName( mConfiguration.DefaultSenderName ) ) mConfiguration.DefaultSenderName = "NPC";

		//	Resources
		string bNpcCustomNamesFilePath = Path.Combine( [ResourcesPath!, "CustomNpcNames", "CustomBNpcNames.csv"] );
		string eNpcCustomNamesFilePath = Path.Combine( [ResourcesPath!, "CustomNpcNames", "CustomENpcNames.csv"] );
		CustomNameDatabase = new( bNpcCustomNamesFilePath, eNpcCustomNamesFilePath );

		//	Localization and Command Initialization
		OnLanguageChanged( mPluginInterface.UiLanguage );

		//	Hooks
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

			nint fpHandleNpcYell = DalamudAPI.SigScanner.ScanText( "E8 ?? ?? ?? ?? F6 43 ?? ?? 0F 85 ?? ?? ?? ?? 0F B7 4B" );
			if( fpHandleNpcYell != IntPtr.Zero )
			{
				var addressInfo = Util.DescribeAddress( fpHandleNpcYell );
				DalamudAPI.PluginLog.Information( $"NpcYell::HandleYell function found at {addressInfo}." );
				mHandleNpcYellHook = DalamudAPI.GameInteropProvider.HookFromAddress<HandleNpcYellDelegate>( fpHandleNpcYell, HandleNpcYellDetour );
				mHandleNpcYellHook?.Enable();
			}
			else
			{
				throw new Exception( "Unable to find the specified function signature for NpcYell::HandleYell." );
			}

			//***** TODO: Remove before release.
			nint fpNpcYellUpdate = DalamudAPI.SigScanner.ScanText( "40 53 55 57 48 83 EC ?? 48 8B 59 ?? 33 ED" );
			if( fpNpcYellUpdate != IntPtr.Zero )
			{
				var addressInfo = Util.DescribeAddress( fpNpcYellUpdate );
				DalamudAPI.PluginLog.Information( $"NpcYell::Update function found at {addressInfo}." );
				mNpcYellUpdateHook = DalamudAPI.GameInteropProvider.HookFromAddress<NpcYellUpdateDelegate>( fpNpcYellUpdate, NpcYellUpdateDetour );
				mNpcYellUpdateHook?.Enable();
			}
			else
			{
				throw new Exception( "Unable to find the specified function signature for NpcYell::Update." );
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
	}

	public void Dispose()
	{
		mOpenBalloonHook?.Disable();
		mOpenBalloonHook?.Dispose();
		mHandleNpcYellHook?.Disable();
		mHandleNpcYellHook?.Dispose();
		mNpcYellUpdateHook?.Disable();
		mNpcYellUpdateHook?.Dispose();

		mConfiguration.Save();

		DalamudAPI.Framework.Update -= OnGameFrameworkUpdate;
		DalamudAPI.ClientState.TerritoryChanged -= OnTerritoryChanged;
		DalamudAPI.ClientState.Logout -= OnLogout;
		DalamudAPI.ClientState.Login -= OnLogin;
		mPluginInterface.LanguageChanged -= OnLanguageChanged;
		mPluginInterface.UiBuilder.Draw -= DrawUI;
		mPluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigUI;
		DalamudAPI.CommandManager.RemoveHandler( TextCommandName );

		mUI?.Dispose();
	}

	private void OnLanguageChanged( string langCode )
	{
		var allowedLang = new List<string>{ /*"es", "fr", "ja"*/ };

		DalamudAPI.PluginLog.Information( "Trying to set up Loc for culture {0}", langCode );

		if( allowedLang.Contains( langCode ) )
		{
			try
			{
				var locPath = Path.Combine( [ ResourcesPath, "Localization", $"loc_{langCode}.json" ] );
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
		if( args.Equals( SubcommandName_Debug, StringComparison.CurrentCultureIgnoreCase ) )
		{
			mUI.Window_Debug.Toggle();
		}
		else if( args.Equals( SubcommandName_Ban, StringComparison.CurrentCultureIgnoreCase ) )
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
		else if( args.Equals( SubcommandName_Unban, StringComparison.CurrentCultureIgnoreCase ) )
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

	private unsafe void OpenBalloonDetour( AgentScreenLog* pAgentScreenLog, Character* pChara, byte* pString, bool softOpen, Int32 parentBoneId )
	{
		//	Do the bare minimum here that we need to track balloons later, both because it is safer to do things in our own framework
		//	event, and because it is cleaner to let the game complete what it is doing before we grab the relevant information.
		try
		{
			DalamudAPI.PluginLog.Verbose( $"OpenBalloon( 0x{(nint)pAgentScreenLog:X}, 0x{(nint)pChara:X}, 0x{(nint)pString:X}, {softOpen}, {parentBoneId} )" );

			if( pChara is null ) DalamudAPI.PluginLog.Warning( $"OpenBalloon called with a null Character*!  This should never happen." );
			else
			{
				var objectID = pChara->GetGameObjectId();
				mObjectIdsWithOpenedBalloons.Add( objectID );
				DalamudAPI.PluginLog.Debug( $"OpenBalloon called for character with object ID 0x{(ulong)objectID:X} (at 0x{(nint)pChara:X})." );
			}
		}
		catch( Exception e )
		{
			DalamudAPI.PluginLog.Error( $"Exception in \"{nameof( OpenBalloonDetour )}\":\r\n{e}" );
		}
		finally
		{
			mOpenBalloonHook?.Original( pAgentScreenLog, pChara, pString, softOpen, parentBoneId );
		}
	}

	private unsafe void HandleNpcYellDetour( NpcYell* pNpcYell, NpcYell.NpcYellInfo* pYellInfo )
	{
		//	Unforutunately, we have to track these too, because the game can also print to log here before it ever gets to a balloon.
		try
		{
			DalamudAPI.PluginLog.Verbose( $"HandleYell( 0x{(nint)pNpcYell:X}, 0x{(nint)pYellInfo:X} )" );

			if( pYellInfo is null ) DalamudAPI.PluginLog.Warning( $"HandleYell called with a null NpcYellInfo*!  This should never happen." );
			else if( pYellInfo->OutputType.HasFlag( NpcYell.NpcYellOutputFlags.PrintToLog ) &&
				pYellInfo->OutputType.HasFlag( NpcYell.NpcYellOutputFlags.ShowBalloon ) &&
				!pYellInfo->OutputType.HasFlag( NpcYell.NpcYellOutputFlags.ShowBattleTalk ) )   //***** TODO: Game blocks printing to log if BattleTalk is set, but BattleTalk may print to the log on its own, so we may have to adjust our logic.
			{
				mObjectIdsWithPrintedNpcYells.Add( pYellInfo->ObjectId );
			}
		}
		catch( Exception e )
		{
			DalamudAPI.PluginLog.Error( $"Exception in \"{nameof( HandleNpcYellDetour )}\":\r\n{e}" );
		}
		finally
		{
			mHandleNpcYellHook?.Original( pNpcYell, pYellInfo );
		}
	}

	//***** TODO: Testing-only.  Remove before releasing plugin.
	private unsafe void NpcYellUpdateDetour( NpcYell* pNpcYell )
	{
		if( pNpcYell is not null && (
			pNpcYell->UnhandledYellCount > 0 ||
			pNpcYell->Unk_2849 > 0 ||
			pNpcYell->Unk_284A ) )
			DalamudAPI.PluginLog.Verbose( $"NpcYell::Update: UnhandledYellCount - {pNpcYell->UnhandledYellCount}, Unk_2849 - {pNpcYell->Unk_2849}, Unk_284A - {pNpcYell->Unk_284A}." );
		mNpcYellUpdateHook?.Original( pNpcYell );
	}

	private unsafe void OnGameFrameworkUpdate( IFramework framework )
	{
		if( !DalamudAPI.ClientState.IsLoggedIn ) return;

		//***** TODO: Move to be a config helper.
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

		//***** TODO: Move to be a config helper.
		float minBalloonTime_Sec = 0f;
		if( mConfiguration.mZoneConfigOverrideDict?.TryGetValue( DalamudAPI.ClientState.TerritoryType, out zoneConfig ) == true )
		{
			minBalloonTime_Sec = zoneConfig.MinBalloonTime_Sec;
		}
		else if( DalamudAPI.Condition[ConditionFlag.BoundByDuty] )
		{
			minBalloonTime_Sec = mConfiguration.MinBalloonTimeInInstance_Sec;
		}
		else
		{
			minBalloonTime_Sec = mConfiguration.MinBalloonTime_Sec;
		}

		//	Clean up any tracked message info that has expired.
		mNPCBalloonHistory.RemoveAll( x => x.IsExpired( messageInfoLifetime ) );

		//	Find any objects with new balloons in the object table.
		var objectsWithNewBalloons = DalamudAPI.ObjectTable.Where( x =>
			x.IsValid() &&
			x.Address != 0 &&
			mObjectIdsWithOpenedBalloons.Contains( x.GameObjectId ) ).ToList();

		//	Finished with this, so clean up for next frame.
		mObjectIdsWithOpenedBalloons.Clear();

		//	Start tracking any new messages.
		foreach( var obj in objectsWithNewBalloons )
		{
			if( obj is ICharacter chara )
			{
				//	Skip PC balloons (if we ever come across any).
				if( chara is IPlayerCharacter )
				{
					DalamudAPI.PluginLog.Debug( $"Skipping balloon on player character object 0x{obj.GameObjectId:X}." );
					continue;
				}

				//	Find the currently in-use balloon text.
				SeString? balloonText = null;
				var pChara = (Character*)chara.Address;
				bool gameHandledPrinting = false;
				if( pChara->YellBalloon.Text.Length > 0 )
				{
					//	Set the PlayTime for the balloon if below configured the minimum.
					if( pChara->YellBalloon.PlayTimer < minBalloonTime_Sec ) pChara->YellBalloon.PlayTimer = minBalloonTime_Sec;

					//	Keep track of whether this is a game-logged balloon.
					if( mObjectIdsWithPrintedNpcYells.Contains( obj.GameObjectId ) )
					{
						gameHandledPrinting = true;
						mObjectIdsWithPrintedNpcYells.Remove( obj.GameObjectId );
					}
					else
					{
						gameHandledPrinting = pChara->YellBalloon.Flags.HasFlag( FFXIVClientStructs.FFXIV.Client.Game.NpcYellBalloonFlags.PrintToLog );
					}

					//	Grab the balloon text.
					balloonText = SeString.Parse( pChara->YellBalloon.Text );
					DalamudAPI.PluginLog.Debug( $"Found valid text for NPCYellBalloon on object 0x{obj.GameObjectId:X}: \"{balloonText.TextValue}\" (Flags:{pChara->YellBalloon.Flags}){( gameHandledPrinting ? " (handled by game)" : string.Empty )}." );
				}
				else if( pChara->Balloon.Text.Length > 0 )
				{
					//	Set the PlayTime for the balloon if below configured the minimum.
					if( pChara->Balloon.PlayTimer < minBalloonTime_Sec ) pChara->Balloon.PlayTimer = minBalloonTime_Sec;

					//	Grab the balloon text.
					balloonText = SeString.Parse( pChara->Balloon.Text );
					DalamudAPI.PluginLog.Debug( $"Found valid text for Balloon on object 0x{obj.GameObjectId:X}: \"{balloonText.TextValue}\"." );
				}
				else
				{
					DalamudAPI.PluginLog.Information( $"Character object 0x{obj.GameObjectId:X} was passed to OpenBalloon, but does not have valid information in any of the known balloon structs." );
				}

				//	Create message info for this balloon if possible, and add or update as appropriate.
				if( balloonText is not null && balloonText.Payloads.Count > 0 )
				{
					var balloonInfo = new NpcMessageInfo()
					{
						SpeakerObjectId = chara.GameObjectId,
						SpeakerKind = chara.ObjectKind,
						SpeakerBaseId = chara.BaseId,
						SpeakerNameId = chara.NameId,
						SpeakerName = chara.Name,
						Message = balloonText,
					};

					if( gameHandledPrinting ) balloonInfo.FlagGameHandledPrinting();

					//***** TODO: Use the configured sender comparison type.
					lock( mNPCBalloonHistoryLockObj )
					{
						var match = mNPCBalloonHistory.FirstOrDefault( x => x.Equals( balloonInfo, true, SenderComparisonType.ObjectID ) );
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
				}
				else
				{
					DalamudAPI.PluginLog.Debug( $"Object with ID 0x{obj:X} opened a balloon, but it contained no valid payloads." );
				}
			}
			else
			{
				DalamudAPI.PluginLog.Warning( $"OpenBalloon was called with an object ({obj:X} ({obj.ObjectKind})) that is not a Character.  This should never happen." );
			}
		}

		//	Try to print any records that still need it.
		lock( mNPCBalloonHistoryLockObj )
		{
			foreach( var balloonMessage in mNPCBalloonHistory.Where( x => !x.HasBeenPrinted ) )
			{
				balloonMessage.PrintToChatLog( CustomNameDatabase, mConfiguration );
			}
		}
	}

	internal static bool IsValidCustomSenderName( SeString? str )
	{
		var textVal = str?.TextValue;
		if( textVal is null ) return false;

		return textVal.Length <= 20 && textVal.All( x =>
		{
			return x >= 'A' && x <= 'Z' ||
					x >= 'a' && x <= 'z' ||
					x is ' ' or '\'' or '-';
		} );
	}

	private void OnTerritoryChanged( UInt16 ID )
	{
		ClearNPCBalloonHistory();
		NpcNameGenerator.CacheCurrentRegion( ID );
	}

	private void OnLogin()
	{
		//UITextColorSelector.CacheUITheme();
	}

	private void OnLogout( int type, int code )
	{
		//UITextColorSelector.CacheUITheme();
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

			DalamudAPI.PluginLog.Information( $"Attempting to export localizable strings to \"{folderPath}\"" );

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
			SpeakerObjectId = 0,
			SpeakerKind = Dalamud.Game.ClientState.Objects.Enums.ObjectKind.None,
			SpeakerBaseId = 0,
			SpeakerNameId = 0,
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

	internal string ResourcesPath => field ??= Path.Combine( mPluginInterface.AssemblyLocation.DirectoryName ?? string.Empty, "Resources" );

	//***** TODO
	//	GameObjects can have names up to 64 bytes long, so we should also limit custom names to that length
	//	in case the log makes any assumptions.  For simplicity, just assume all characters are double-width.
	private const string ValidSenderNamePattern = @"\A[\p{L}\p{N}\p{P}\p{Zs}\p{Co}\●^`]{0,32}\z";

	private readonly PluginUI mUI;
	private readonly Configuration mConfiguration;

	internal NpcNameDatabase CustomNameDatabase { get; private init; }

	private unsafe delegate void OpenBalloonDelegate( AgentScreenLog* pAgentScreenLog, Character* pChara, byte* pString, bool softOpen, Int32 parentBoneId );
	private readonly Hook<OpenBalloonDelegate>? mOpenBalloonHook = null;

	private unsafe delegate void HandleNpcYellDelegate( NpcYell* pNpcYell, NpcYell.NpcYellInfo* pYellInfo );
	private readonly Hook<HandleNpcYellDelegate>? mHandleNpcYellHook = null;

	private unsafe delegate void NpcYellUpdateDelegate( NpcYell* pNpcYell );
	private readonly Hook<NpcYellUpdateDelegate>? mNpcYellUpdateHook = null;

	private readonly Lock mNPCBalloonHistoryLockObj = new();
	private readonly List<NpcMessageInfo> mNPCBalloonHistory = new();

	private readonly HashSet<GameObjectId> mObjectIdsWithOpenedBalloons = new();
	private readonly HashSet<GameObjectId> mObjectIdsWithPrintedNpcYells = new();

	private readonly IDalamudPluginInterface mPluginInterface;
}
