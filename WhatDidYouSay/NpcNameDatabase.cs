using System;
using System.Diagnostics;

namespace WhatDidYouSay;

internal class NpcNameDatabase
{
	public NpcNameDatabase( string bNpcFilePath, string eNpcFilePath )
	{
		mBNpcNames = new( bNpcFilePath, out var loadedBNpcNameFile );
		mENpcNames = new( eNpcFilePath, out var loadedENpcNameFile );

		if( !loadedBNpcNameFile ) DalamudAPI.PluginLog.Warning( $"Unable to load file \"{bNpcFilePath}\"!  Custom BNpc names will be unavailable." );
		if( !loadedENpcNameFile ) DalamudAPI.PluginLog.Warning( $"Unable to load file \"{eNpcFilePath}\"!  Custom ENpc names will be unavailable." );
	}

	public bool TryGetBNpcName( UInt32 bNpcBaseId, UInt32 nameId, out string name )
	{
		DalamudAPI.PluginLog.Verbose( $"Checking for custom name for bNPC {bNpcBaseId}, {nameId}." );
		mBNpcNameLookupStopwatch.Restart();

		var customName = mBNpcNames[bNpcBaseId, nameId];
		if( !string.IsNullOrWhiteSpace( customName ) )
		{
			name = customName;
			DalamudAPI.PluginLog.Verbose( $"Name lookup succeeded (\"{name}\") in {mBNpcNameLookupStopwatch.ElapsedMicroseconds}μs." );
			return true;
		}
		else
		{
			name = string.Empty;
			DalamudAPI.PluginLog.Verbose( $"Name lookup failed in {mBNpcNameLookupStopwatch.ElapsedMicroseconds}μs." );
			return false;
		}
	}

	public bool TryGetENpcName( UInt32 eNpcBaseId, out string name )
	{
		DalamudAPI.PluginLog.Verbose( $"Checking for custom name for eNPC {eNpcBaseId}." );
		mENpcNameLookupStopwatch.Restart();

		var customName = mENpcNames[eNpcBaseId];
		if( !string.IsNullOrWhiteSpace( customName ) )
		{
			name = customName;
			DalamudAPI.PluginLog.Verbose( $"Name lookup succeeded (\"{name}\") in {mENpcNameLookupStopwatch.ElapsedMicroseconds}μs." );
			return true;
		}
		else
		{
			name = string.Empty;
			DalamudAPI.PluginLog.Verbose( $"Name lookup failed in {mENpcNameLookupStopwatch.ElapsedMicroseconds}μs." );
			return false;
		}
	}

	protected readonly CustomNpcNameFile mBNpcNames;
	protected readonly CustomNpcNameFile mENpcNames;
	protected readonly Stopwatch mBNpcNameLookupStopwatch = new();
	protected readonly Stopwatch mENpcNameLookupStopwatch = new();
}
