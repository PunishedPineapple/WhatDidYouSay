using System;
using System.Buffers.Binary;

using CheapLoc;

namespace WhatDidYouSay;

public enum SenderComparisonType : int
{
	ObjectId = 0,
	BaseId = 1,
	NameId_FallbackBaseId = 2,
	NameId_FallbackText = 3,
	NameText = 4,
	NeverEqual = 5,
}

internal static class SenderComparisonTypeExtensions
{
	extension( SenderComparisonType comparisonType )
	{
		public string GetTranslatedName()
		{
			return comparisonType switch
			{
				SenderComparisonType.ObjectId => Loc.Localize( "Enum Value: SenderComparisonType - ObjectId", "Object ID" ),
				SenderComparisonType.BaseId => Loc.Localize( "Enum Value: SenderComparisonType - BaseId", "Base ID" ),
				SenderComparisonType.NameId_FallbackBaseId => Loc.Localize( "Enum Value: SenderComparisonType - NameId_FallbackBaseId", "Name Id (Fallback to Base ID)" ),
				SenderComparisonType.NameId_FallbackText => Loc.Localize( "Enum Value: SenderComparisonType - NameId_FallbackText", "Name ID (Fallback to Name Text)" ),
				SenderComparisonType.NameText => Loc.Localize( "Enum Value: SenderComparisonType - NameText", "Name Text" ),
				SenderComparisonType.NeverEqual => Loc.Localize( "Enum Value: SenderComparisonType - NeverEqual", "Never Equal" ),
				_ => "INVALID VALUE"
			};
		}
	}
}