using System;

using Dalamud.Game;

namespace WhatDidYouSay;

internal class CustomNpcNameInfo
{
	public UInt32 NpcBaseId { get; init; } = 0;
	public UInt32 NpcNameId { get; init; } = 0;
	public string Ja { get; init; } = string.Empty;
	public string En { get; init; } = string.Empty;
	public string De { get; init; } = string.Empty;
	public string Fr { get; init; } = string.Empty;

	public string? this[ClientLanguage lang]
	{
		get
		{
			return lang switch
			{
				Dalamud.Game.ClientLanguage.Japanese => Ja,
				Dalamud.Game.ClientLanguage.English => En,
				Dalamud.Game.ClientLanguage.German => De,
				Dalamud.Game.ClientLanguage.French => Fr,
				_ => null,
			};
		}
	}
}
