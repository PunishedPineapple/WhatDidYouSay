using System;

using Lumina.Excel.Sheets;

namespace WhatDidYouSay;

internal static partial class NpcNameGenerator
{
	public static RaceNameRegion CurrentRegion { get; private set; } = RaceNameRegion.Default;

	public static void CacheCurrentRegion( UInt32 territoryType )
	{
		CurrentRegion = GetRaceNameRegionForTerritory( territoryType );
	}

	public static bool TryGenerateNameFromBNpcId( UInt32 BNpcBaseId, out string name, RaceNameKind raceKind = RaceNameKind.Tribe, bool reducedGender = false )
	{
		name = string.Empty;

		var BNpcSheet = DalamudAPI.DataManager.GetExcelSheet<BNpcBase>();
		if( BNpcSheet.TryGetRow( BNpcBaseId, out var row ) && row.BNpcCustomize.IsValid && row.BNpcCustomize.Value.Race.IsValid && row.BNpcCustomize.Value.Tribe.IsValid )
		{
			var race = row.BNpcCustomize.Value.Race.RowId;
			var tribe = row.BNpcCustomize.Value.Tribe.RowId;
			var gender = row.BNpcCustomize.Value.Gender;
			var headEquipId = row.NpcEquip.IsValid ? row.NpcEquip.Value.ModelHead : 0;

			return TryGenerateNameFromCustomizeData( race, tribe, gender, out name, raceKind, reducedGender, headEquipId );
		}

		return false;
	}

	public static bool TryGenerateNameFromENpcId( UInt32 ENpcBaseId, out string name, RaceNameKind raceKind = RaceNameKind.Tribe, bool reducedGender = false )
	{
		name = string.Empty;

		var ENpcSheet = DalamudAPI.DataManager.GetExcelSheet<ENpcBase>();
		if( ENpcSheet.TryGetRow( ENpcBaseId, out var row ) && row.Race.IsValid && row.Tribe.IsValid )
		{
			var race = row.Race.RowId;
			var tribe = row.Tribe.RowId;
			var gender = row.Gender;
			var headEquipId = row.ModelHead;

			return TryGenerateNameFromCustomizeData( race, tribe, gender, out name, raceKind, reducedGender, headEquipId );
		}

		return false;
	}

	private static bool TryGenerateNameFromCustomizeData( UInt32 race, UInt32 tribe, byte gender, out string name, RaceNameKind raceKind = RaceNameKind.Tribe, bool reducedGender = false, UInt32 headEquipId = 0 )
	{
		name = string.Empty;

		//***** TODO: Is there a more reliable way to distunguish Pelu from regular Hyur?
		if( mPeluHeadEquipIds.Contains( headEquipId ) )
		{
			name = GetPeluName( DalamudAPI.ClientState.ClientLanguage );
			return true;
		}

		var currentLangNpcNames = DalamudAPI.ClientState.ClientLanguage switch
		{
			Dalamud.Game.ClientLanguage.Japanese => mNpcNames_Ja,
			Dalamud.Game.ClientLanguage.German => mNpcNames_De,
			Dalamud.Game.ClientLanguage.French => mNpcNames_Fr,
			_ => mNpcNames_En,
		};

		if( (int)CurrentRegion < 0 || (int)CurrentRegion >= currentLangNpcNames.Length )
		{
			DalamudAPI.PluginLog.Warning( $"Unexpected region \"{CurrentRegion}\" in {nameof( TryGenerateNameFromENpcId )}." );
			CurrentRegion = RaceNameRegion.Default;
		}

		var namesForRegion = currentLangNpcNames[(int)CurrentRegion];
		if( tribe >= namesForRegion.Length ) return false;
		var names = namesForRegion[tribe];
		if( names is null ) return false;

		if( gender == 1 )
		{
			if( reducedGender )
			{
				name = raceKind == RaceNameKind.Tribe ? names.Value.tribe_FeminineReduced : names.Value.race_FeminineReduced;
				return true;
			}
			else
			{
				name = raceKind == RaceNameKind.Tribe ? names.Value.tribe_Feminine : names.Value.race_Feminine;
				return true;
			}
		}
		else
		{
			if( reducedGender )
			{
				name = raceKind == RaceNameKind.Tribe ? names.Value.tribe_MasculineReduced : names.Value.race_MasculineReduced;
				return true;
			}
			else
			{
				name = raceKind == RaceNameKind.Tribe ? names.Value.tribe_Masculine : names.Value.race_Masculine;
				return true;
			}
		}
	}

	private static RaceNameRegion GetRaceNameRegionForTerritory( UInt32 territoryType )
	{
		if( mTerritoryTypeKludges_TheNinth.Contains( territoryType ) ) return RaceNameRegion.TheNinth;
		else if( mTerritoryTypeKludges_Tural.Contains( territoryType ) ) return RaceNameRegion.Tural;
		else if( mTerritoryTypeKludges_TheFirst.Contains( territoryType ) ) return RaceNameRegion.TheFirst;

		var territorySheet = DalamudAPI.DataManager.GetExcelSheet<TerritoryType>();
		if( territorySheet.TryGetRow( territoryType, out var row ) && row.PlaceNameRegion.IsValid )
		{
			if( mPlaceNameRegions_TheNinth.Contains( row.PlaceNameRegion.RowId ) ) return RaceNameRegion.TheNinth;
			else if( mPlaceNameRegions_Tural.Contains( row.PlaceNameRegion.RowId ) ) return RaceNameRegion.Tural;
			else if( mPlaceNameRegions_TheFirst.Contains( row.PlaceNameRegion.RowId ) ) return RaceNameRegion.TheFirst;
		}

		return RaceNameRegion.Default;
	}
}
