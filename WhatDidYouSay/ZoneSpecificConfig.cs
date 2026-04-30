using System;

using Lumina.Excel.Sheets;

namespace WhatDidYouSay;

public class ZoneSpecificConfig
{
	public bool DisableForZone = false;
	public bool InheritRepeatsAllowed = true;
	public bool RepeatsAllowed = true;
	public bool InheritTimeBeforeRepeatsAllowed = true;
	public int TimeBeforeRepeatsAllowed_Sec = 10;
	public bool InheritMinBalloonTime = true;
	public float MinBalloonTime_Sec = 0f;
	public bool InheritSenderComparisonType_ENpc = true;
	public SenderComparisonType SenderComparisonType_ENpc = SenderComparisonType.BaseId;
	public bool InheritSenderComparisonType_BNpc = true;
	public SenderComparisonType SenderComparisonType_BNpc = SenderComparisonType.NameId_FallbackBaseId;

	//***** TODO: Enum helpers.

	//***** TODO: Set inherit flags for existing configs if both of the old fields match the default.

	internal TimeSpan TimeBeforeRepeatsAllowed => TimeSpan.FromSeconds( TimeBeforeRepeatsAllowed_Sec );

	public static string GetNiceNameForZone( UInt32 territoryType )
	{
		var territoryTypeSheet = DalamudAPI.DataManager.GetExcelSheet<TerritoryType>();

		if( !territoryTypeSheet.HasRow( territoryType ) )
		{
			return $"Invalid Territory ({territoryType})";
		}

		var territoryTypeForZone = territoryTypeSheet.GetRow( territoryType );
		var contentFinderConditionName = territoryTypeForZone.ContentFinderCondition.IsValid ? territoryTypeForZone.ContentFinderCondition.Value.Name.ExtractText()?.Trim() : string.Empty;

		if( !string.IsNullOrWhiteSpace( contentFinderConditionName ) )
		{
			return contentFinderConditionName;
		}
		else if( territoryTypeForZone.PlaceName.IsValid &&
				 territoryTypeForZone.PlaceName.Value.Name.ExtractText()?.Trim() is string territoryTypeName &&
				 territoryTypeName.Length > 0 )
		{
			return territoryTypeName;
		}
		else
		{
			return $"Territory {territoryType}";
		}
	}
}
