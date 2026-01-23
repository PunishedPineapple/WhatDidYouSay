using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using SimpleCSV;

namespace WhatDidYouSay;

internal class CustomNpcNameFile
{
	public CustomNpcNameFile( string filePath, out bool loaded )
	{
		loaded = false;
		var tempDict = new SortedDictionary<CustomNpcNameId,CustomNpcNameInfo>();

		if( Path.Exists( filePath ) )
		{
			try
			{
				var reader = CSVReader.ParseFromFile( filePath, true );

				var idColumnIndex = reader.GetColumnIndexForName( "NpcId", true );
				var nameIdColumnIndex = reader.GetColumnIndexForName( "NameId", true );
				var jaColumnIndex = reader.GetColumnIndexForName( "ja", true );
				var enColumnIndex = reader.GetColumnIndexForName( "en", true );
				var deColumnIndex = reader.GetColumnIndexForName( "de", true );
				var frColumnIndex = reader.GetColumnIndexForName( "fr", true );

				if( idColumnIndex < 0 ) DalamudAPI.PluginLog.Error( $"Unable to find \"NpcId\" column in file \"{filePath}\"!" );
				else if( jaColumnIndex < 0 ) DalamudAPI.PluginLog.Error( $"Unable to find \"ja\" column in file \"{filePath}\"!" );
				else if( enColumnIndex < 0 ) DalamudAPI.PluginLog.Error( $"Unable to find \"en\" column in file \"{filePath}\"!" );
				else if( deColumnIndex < 0 ) DalamudAPI.PluginLog.Error( $"Unable to find \"de\" column in file \"{filePath}\"!" );
				else if( frColumnIndex < 0 ) DalamudAPI.PluginLog.Error( $"Unable to find \"fr\" column in file \"{filePath}\"!" );
				else
				{
					bool hasNameIdColumn = nameIdColumnIndex >= 0;

					foreach( var row in reader )
					{
						string idStr = row[idColumnIndex];
						string? nameIdStr = hasNameIdColumn ? row[nameIdColumnIndex] : null;
						if( !UInt32.TryParse( idStr, out var id ) | hasNameIdColumn & !UInt32.TryParse( nameIdStr, out var nameId ) )
						{
							string combinedIdStr = idStr + ( hasNameIdColumn ? $", {nameIdStr}" : string.Empty );
							DalamudAPI.PluginLog.Warning( $"{combinedIdStr} is not a valid NPC ID!  Skipping row." );
						}
						else
						{
							bool badName = false;
							foreach( var entry in row )
							{
								//***** TODO: We may want to allow certain very specific non-text payloads (i.e., italics).
								if( entry.Where( x => x < '\x20' ).Any() )
								{
									DalamudAPI.PluginLog.Warning( $"ASCII control characters found in name \"{entry}\".  Ignoring custom names for this NPC." );
									badName = true;
								}
							}

							if( !badName )
							{
								var combinedId = new CustomNpcNameId( id, hasNameIdColumn ? nameId : 0 );
								DalamudAPI.PluginLog.Verbose( $"Trying to add custom NPC names for ID {combinedId}: {string.Join( ',', row )}" );
								tempDict.Add( combinedId, new()
								{
									NpcBaseId = id,
									NpcNameId = hasNameIdColumn ? nameId : 0,
									Ja = row[jaColumnIndex],
									En = row[enColumnIndex],
									De = row[deColumnIndex],
									Fr = row[frColumnIndex],
								} );
							}
						}
					}

					loaded = true;
					DalamudAPI.PluginLog.Information( $"Loaded {tempDict.Count} custom NPC name rows from \"{filePath}\"." );
				}
			}
			catch( Exception e )
			{
				DalamudAPI.PluginLog.Error( $"Exception while trying to parse \"{filePath}\":\r\n{e}" );
			}
		}
		else
		{
			DalamudAPI.PluginLog.Information( $"File \"{filePath}\" does not exist!" );
		}

		mCustomNames = tempDict.ToFrozenDictionary();
	}

	public string? this[UInt32 NpcId] => this[(CustomNpcNameId)NpcId];
	public string? this[UInt32 NpcId, UInt32 nameId] => this[new CustomNpcNameId( NpcId, nameId )];
	public string? this[CustomNpcNameId NpcId]
	{
		get
		{
			string? retVal = null;
			bool foundNpc = mCustomNames.TryGetValue( NpcId, out var customName );

			if( foundNpc && customName is not null )
			{
				retVal = customName[DalamudAPI.ClientState.ClientLanguage];
			}

			return retVal;
		}
	}

	protected readonly FrozenDictionary<CustomNpcNameId,CustomNpcNameInfo> mCustomNames;
}