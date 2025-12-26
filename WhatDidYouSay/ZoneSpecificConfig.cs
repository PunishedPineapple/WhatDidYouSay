using System;

using Lumina.Excel.Sheets;

namespace WhatDidYouSay;

public class ZoneSpecificConfig
{
	public bool DisableForZone = false;
	public bool RepeatsAllowed = true;
	public int TimeBeforeRepeatsAllowed_Sec = 10;
	public float MinBalloonTime_Sec = 0f;

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
