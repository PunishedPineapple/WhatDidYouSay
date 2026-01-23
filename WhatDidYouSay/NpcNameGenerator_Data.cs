using System;

using Dalamud.Game;

namespace WhatDidYouSay;

internal static partial class NpcNameGenerator
{
	public static readonly UInt32[] mPlaceNameRegions_TheFirst = [2950];
	public static readonly UInt32[] mPlaceNameRegions_Tural = [4500, 4501];
	public static readonly UInt32[] mPlaceNameRegions_TheNinth = [4502];

	//	Territories that are not technically part of the region, but should be considered as such for naming.
	public static readonly UInt32[] mTerritoryTypeKludges_TheFirst = [];    //***** TODO
	public static readonly UInt32[] mTerritoryTypeKludges_Tural = [];   //***** TODO
	public static readonly UInt32[] mTerritoryTypeKludges_TheNinth =
	[
		1186,	//	Solution 9
		1200,	//	Everkeep
		1201,	//	Everkeep
		1208,	//	Origenics
		1213,	//	Solution 9
		1220,	//	Everkeep
		1224,	//	Greenroom
		1225,	//	m1n
		1226,	//	m1s
		1227,	//	m2n
		1228,	//	m2s
		1229,	//	m3n
		1230,	//	m3s
		1231,	//	m4n
		1232,	//	m4s
		1242,	//	Yuweyawata
		1243,	//	Interphos Ex
		1254,	//	Yuweyawata
		1256,	//	m5n
		1257,	//	m5s
		1258,	//	m6n
		1259,	//	m6s
		1260,	//	m7n
		1261,	//	m7s
		1262,	//	m8n
		1263,	//	m8s
		1266,	//	Underkeep
		1268,	//	Break Room
		1270,	//	Recollection
		1271,	//	Recollection Ex
		1274,	//	Throne Room
		1275,	//	Recollection
		1320,	//	m9n
		1321,	//	m9s
		1322,	//	m10n
		1323,	//	m10s
		1324,	//	m11n
		1325,	//	m11s
		1326,	//	m12n
		1327,	//	m12s
	];

	private static readonly UInt32[] mPeluHeadEquipIds = [74781];

	private static string GetPeluName( ClientLanguage lang )
	{
		return lang switch
		{
			ClientLanguage.Japanese => "ペルペル族", //***** TODO: Have someone verify this.
			ClientLanguage.German => "Pelupelu",
			ClientLanguage.French => "Pelupelu",
			_ => "Pelupelu",
		};
	}

	//***** TODO
	private static readonly (string race_Masculine, string race_Feminine, string race_MasculineReduced, string race_FeminineReduced, string tribe_Masculine, string tribe_Feminine, string tribe_MasculineReduced, string tribe_FeminineReduced)?[][]
	mNpcNames_Ja =
	[
		[
			//	Default
			null,	//	Invalid Race
			( "Hyuran Man", "Hyuran Woman", "Hyur", "Hyur", "Midlander Man", "Midlander Woman", "Midlander", "Midlander"),	//	Midlander
			( "Hyuran Man", "Hyuran Woman", "Hyur", "Hyur", "Highlander Man", "Highlander Woman", "Highlander", "Highlander" ),	//	Highlander
			( "Elezen Man", "Elezen Woman", "Elezen", "Elezen", "Wildwood Man", "Wildwood Woman", "Wildwood", "Wildwood" ),	//	Wildwood
			( "Elezen Man", "Elezen Woman", "Elezen", "Elezen", "Duskwight Man", "Duskwight Woman", "Duskwight", "Duskwight" ),	//	Duskwight
			( "Lalafellin Man", "Lalafellin Woman", "Lalafell", "Lalafell", "Plainsfolk Man", "Plainsfolk Woman", "Plainsfolk", "Plainsfolk" ),	//	Plainsfolk
			( "Lalafellin Man", "Lalafellin Woman", "Lalafell", "Lalafell", "Dunesfolk Man", "Dunesfolk Woman", "Dunesfolk", "Dunesfolk" ),	//	Dunesfolk
			( "Miqo'te Man", "Miqo'te Woman", "Miqo'te", "Miqo'te", "Male Seeker", "Female Seeker", "Seeker of the Sun", "Seeker of the Sun" ),	//	Seekers
			( "Miqo'te Man", "Miqo'te Woman", "Miqo'te", "Miqo'te", "Male Keeper", "Female Keeper", "Keeper of the Moon", "Keeper of the Moon" ),	//	Keepers
			( "Roegadyn Man", "Roegadyn Woman", "Roegadyn", "Roegadyn", "Male Sea Wolf", "Female Sea Wolf", "Sea Wolf", "Sea Wolf" ),	//	Sea Wolf
			( "Roegadyn Man", "Roegadyn Woman", "Roegadyn", "Roegadyn", "Hellsguard Man", "Hellsguard Woman", "Hellsguard", "Hellsguard" ),	//	Hellsguard
			( "Au Ra Man", "Au Ra Woman", "Au Ra", "Au Ra", "Raen Man", "Raen Woman", "Raen", "Raen" ),	//	Raen
			( "Au Ra Man", "Au Ra Woman", "Au Ra", "Au Ra", "Xaela Man", "Xaela Woman", "Xaela", "Xaela" ),	//	Xaela
			( "Hrothgar Man", "Hrothgar Woman", "Hrothgar", "Hrothgar", "Male Helion", "Female Helion", "Helion", "Helion" ),	//	Helion
			( "Hrothgar Man", "Hrothgar Woman", "Hrothgar", "Hrothgar", "Lost Man", "Lost Woman", "Lost", "Lost" ),	//	The Lost
			( "Viera Man", "Viera Woman", "Viera", "Viera", "Rava Man", "Rava Woman", "Rava", "Rava" ),	//	Rava
			( "Viera Man", "Viera Woman", "Viera", "Viera", "Veena Man", "Veena Woman", "Veena", "Veena" ),	//	Veena
		],
		[
			//	The First
			null,	//	Invalid Race
			( "Hume Man", "Hume Woman", "Hume", "Hume", "Hume Man", "Hume Woman", "Hume", "Hume" ),	//	Midlander
			( "Hume Man", "Hume Woman", "Hume", "Hume", "Hume Man", "Hume Woman", "Hume", "Hume" ),	//	Highlander
			( "Elven Man", "Elven Woman", "Elf", "Elf", "Elven Man", "Elven Woman", "Elf", "Elf" ),	//	Wildwood
			( "Elven Man", "Elven Woman", "Elf", "Elf", "Elven Man", "Elven Woman", "Elf", "Elf" ),	//	Duskwight
			( "Dwarven Man", "Dwarven Woman", "Dwarf", "Dwarf", "Dwarven Man", "Dwarven Woman", "Dwarf", "Dwarf" ),	//	Plainsfolk
			( "Dwarven Man", "Dwarven Woman", "Dwarf", "Dwarf", "Dwarven Man", "Dwarven Woman", "Dwarf", "Dwarf" ),	//	Dunesfolk
			( "Mystel Man", "Mystel Woman", "Mystel", "Mystel", "Mystel Man", "Mystel Woman", "Mystel", "Mystel" ),	//	Seekers
			( "Mystel Man", "Mystel Woman", "Mystel", "Mystel", "Mystel Man", "Mystel Woman", "Mystel", "Mystel" ),	//	Keepers
			( "Galdjent Man", "Galdjent Woman", "Galdjent", "Galdjent", "Galdjent Man", "Galdjent Woman", "Galdjent", "Galdjent" ),	//	Sea Wolf
			( "Galdjent Man", "Galdjent Woman", "Galdjent", "Galdjent", "Galdjent Man", "Galdjent Woman", "Galdjent", "Galdjent" ),	//	Hellsguard
			( "Drahn Man", "Drahn Woman", "Drahn", "Drahn", "Drahn Man", "Drahn Woman", "Drahn", "Drahn" ),	//	Raen
			( "Drahn Man", "Drahn Woman", "Drahn", "Drahn", "Drahn Man", "Drahn Woman", "Drahn", "Drahn" ),	//	Xaela
			( "Ronso Man", "Ronso Woman", "Ronso", "Ronso", "Ronso Man", "Ronso Woman", "Ronso", "Ronso" ),	//	Helion
			( "Ronso Man", "Ronso Woman", "Ronso", "Ronso", "Ronso Man", "Ronso Woman", "Ronso", "Ronso" ),	//	The Lost
			( "Viis Man", "Viis Woman", "Viis", "Viis", "Rava Man", "Rava Woman", "Rava", "Rava" ),	//	Rava
			( "Viis Man", "Viis Woman", "Viis", "Viis", "Veena Man", "Veena Woman", "Veena", "Veena" ),	//	Veena
		],
		[
			//	Tural
			null,	//	Invalid Race
			( "Tonawawtan Man", "Tonawawtan Woman", "Tonawawta", "Tonawawta", "Tonawawtan Man", "Tonawawtan Woman", "Tonawawta", "Tonawawta" ),	//	Midlander
			( "Tonawawtan Man", "Tonawawtan Woman", "Tonawawta", "Tonawawta", "Tonawawtan Man", "Tonawawtan Woman", "Tonawawta", "Tonawawta" ),	//	Highlander
			( "Elezen Man", "Elezen Woman", "Elezen", "Elezen", "Wildwood Man", "Wildwood Woman", "Wildwood", "Wildwood" ),	//	Wildwood
			( "Elezen Man", "Elezen Woman", "Elezen", "Elezen", "Duskwight Man", "Duskwight Woman", "Duskwight", "Duskwight" ),	//	Duskwight
			( "Lalafellin Man", "Lalafellin Woman", "Lalafell", "Lalafell", "Plainsfolk Man", "Plainsfolk Woman", "Plainsfolk", "Plainsfolk" ),	//	Plainsfolk
			( "Lalafellin Man", "Lalafellin Woman", "Lalafell", "Lalafell", "Dunesfolk Man", "Dunesfolk Woman", "Dunesfolk", "Dunesfolk" ),	//	Dunesfolk
			( "Hhetsarro Man", "Hhetsarro Woman", "Hhetsarro", "Hhetsarro", "Hhetsarro Man", "Hhetsarro Woman", "Hhetsarro", "Hhetsarro" ),	//	Seekers
			( "Hhetsarro Man", "Hhetsarro Woman", "Hhetsarro", "Hhetsarro", "Hhetsarro Man", "Hhetsarro Woman", "Hhetsarro", "Hhetsarro" ),	//	Keepers
			( "Roegadyn Man", "Roegadyn Woman", "Roegadyn", "Roegadyn", "Male Sea Wolf", "Female Sea Wolf", "Sea Wolf", "Sea Wolf" ),	//	Sea Wolf
			( "Roegadyn Man", "Roegadyn Woman", "Roegadyn", "Roegadyn", "Hellsguard Man", "Hellsguard Woman", "Hellsguard", "Hellsguard" ),	//	Hellsguard
			( "Au Ra Man", "Au Ra Woman", "Au Ra", "Au Ra", "Raen Man", "Raen Woman", "Raen", "Raen" ),	//	Raen
			( "Au Ra Man", "Au Ra Woman", "Au Ra", "Au Ra", "Xaela Man", "Xaela Woman", "Xaela", "Xaela" ),	//	Xaela
			( "Xbr'aal Man", "Xbr'aal Woman", "Xbr'aal", "Xbr'aal", "Xbr'aal Man", "Xbr'aal Woman", "Xbr'aal", "Xbr'aal" ),	//	Helion
			( "Xbr'aal Man", "Xbr'aal Woman", "Xbr'aal", "Xbr'aal", "Xbr'aal Man", "Xbr'aal Woman", "Xbr'aal", "Xbr'aal" ),	//	The Lost
			( "Shetona Man", "Shetona Woman", "Shetona", "Shetona", "Shetona Man", "Shetona Woman", "Shetona", "Shetona" ),	//	Rava
			( "Shetona Man", "Shetona Woman", "Shetona", "Shetona", "Shetona Man", "Shetona Woman", "Shetona", "Shetona" ),	//	Veena
		],
		[
			//	The Ninth
			null,	//	Invalid Race
			( "Hyune Man", "Hyune Woman", "Hyune", "Hyune", "Hyune Man", "Hyune Woman", "Hyune", "Hyune" ),	//	Midlander
			( "Hyune Man", "Hyune Woman", "Hyune", "Hyune", "Hyune Man", "Hyune Woman", "Hyune", "Hyune" ),	//	Highlander
			( "Eldite Man", "Eldite Woman", "Eldite", "Eldite", "Eldite Man", "Eldite Woman", "Eldite", "Eldite" ),	//	Wildwood
			( "Eldite Man", "Eldite Woman", "Eldite", "Eldite", "Eldite Man", "Eldite Woman", "Eldite", "Eldite" ),	//	Duskwight
			( "Male Milalla", "Female Milalla", "Milalla", "Milalla", "Male Milalla", "Female Milalla", "Milalla", "Milalla" ),	//	Plainsfolk
			( "Male Milalla", "Female Milalla", "Milalla", "Milalla", "Male Milalla", "Female Milalla", "Milalla", "Milalla" ),	//	Dunesfolk
			( "Hhetsarro Man", "Hhetsarro Woman", "Hhetsarro", "Hhetsarro", "Male Hhetsarro", "Female Hhetsarro", "Hhetsarro", "Hhetsarro" ),	//	Seekers
			( "Hhetsarro Man", "Hhetsarro Woman", "Hhetsarro", "Hhetsarro", "Male Hhetsarro", "Female Hhetsarro", "Hhetsarro", "Hhetsarro" ),	//	Keepers
			( "Roegadyn Man", "Roegadyn Woman", "Roegadyn", "Roegadyn", "Male Sea Wolf", "Female Sea Wolf", "Sea Wolf", "Sea Wolf" ),	//	Sea Wolf
			( "Roegadyn Man", "Roegadyn Woman", "Roegadyn", "Roegadyn", "Hellsguard Man", "Hellsguard Woman", "Hellsguard", "Hellsguard" ),	//	Hellsguard
			( "Au Ra Man", "Au Ra Woman", "Au Ra", "Au Ra", "Raen Man", "Raen Woman", "Raen", "Raen" ),	//	Raen
			( "Au Ra Man", "Au Ra Woman", "Au Ra", "Au Ra", "Xaela Man", "Xaela Woman", "Xaela", "Xaela" ),	//	Xaela
			( "Xbr'aal Man", "Xbr'aal Woman", "Xbr'aal", "Xbr'aal", "Xbr'aal Man", "Xbr'aal Woman", "Xbr'aal", "Xbr'aal" ),	//	Helion
			( "Xbr'aal Man", "Xbr'aal Woman", "Xbr'aal", "Xbr'aal", "Xbr'aal Man", "Xbr'aal Woman", "Xbr'aal", "Xbr'aal" ),	//	The Lost
			( "Shetona Man", "Shetona Woman", "Shetona", "Shetona", "Shetona Man", "Shetona Woman", "Shetona", "Shetona" ),	//	Rava
			( "Shetona Man", "Shetona Woman", "Shetona", "Shetona", "Shetona Man", "Shetona Woman", "Shetona", "Shetona" ),	//	Veena
		],
	];

	private static readonly (string race_Masculine, string race_Feminine, string race_MasculineReduced, string race_FeminineReduced, string tribe_Masculine, string tribe_Feminine, string tribe_MasculineReduced, string tribe_FeminineReduced)?[][]
	mNpcNames_En =
	[
		[
			//	Default
			null,	//	Invalid Race
			( "Hyuran Man", "Hyuran Woman", "Hyur", "Hyur", "Midlander Man", "Midlander Woman", "Midlander", "Midlander"),	//	Midlander
			( "Hyuran Man", "Hyuran Woman", "Hyur", "Hyur", "Highlander Man", "Highlander Woman", "Highlander", "Highlander" ),	//	Highlander
			( "Elezen Man", "Elezen Woman", "Elezen", "Elezen", "Wildwood Man", "Wildwood Woman", "Wildwood", "Wildwood" ),	//	Wildwood
			( "Elezen Man", "Elezen Woman", "Elezen", "Elezen", "Duskwight Man", "Duskwight Woman", "Duskwight", "Duskwight" ),	//	Duskwight
			( "Lalafellin Man", "Lalafellin Woman", "Lalafell", "Lalafell", "Plainsfolk Man", "Plainsfolk Woman", "Plainsfolk", "Plainsfolk" ),	//	Plainsfolk
			( "Lalafellin Man", "Lalafellin Woman", "Lalafell", "Lalafell", "Dunesfolk Man", "Dunesfolk Woman", "Dunesfolk", "Dunesfolk" ),	//	Dunesfolk
			( "Miqo'te Man", "Miqo'te Woman", "Miqo'te", "Miqo'te", "Male Seeker", "Female Seeker", "Seeker of the Sun", "Seeker of the Sun" ),	//	Seekers
			( "Miqo'te Man", "Miqo'te Woman", "Miqo'te", "Miqo'te", "Male Keeper", "Female Keeper", "Keeper of the Moon", "Keeper of the Moon" ),	//	Keepers
			( "Roegadyn Man", "Roegadyn Woman", "Roegadyn", "Roegadyn", "Male Sea Wolf", "Female Sea Wolf", "Sea Wolf", "Sea Wolf" ),	//	Sea Wolf
			( "Roegadyn Man", "Roegadyn Woman", "Roegadyn", "Roegadyn", "Hellsguard Man", "Hellsguard Woman", "Hellsguard", "Hellsguard" ),	//	Hellsguard
			( "Au Ra Man", "Au Ra Woman", "Au Ra", "Au Ra", "Raen Man", "Raen Woman", "Raen", "Raen" ),	//	Raen
			( "Au Ra Man", "Au Ra Woman", "Au Ra", "Au Ra", "Xaela Man", "Xaela Woman", "Xaela", "Xaela" ),	//	Xaela
			( "Hrothgar Man", "Hrothgar Woman", "Hrothgar", "Hrothgar", "Male Helion", "Female Helion", "Helion", "Helion" ),	//	Helion
			( "Hrothgar Man", "Hrothgar Woman", "Hrothgar", "Hrothgar", "Lost Man", "Lost Woman", "Lost", "Lost" ),	//	The Lost
			( "Viera Man", "Viera Woman", "Viera", "Viera", "Rava Man", "Rava Woman", "Rava", "Rava" ),	//	Rava
			( "Viera Man", "Viera Woman", "Viera", "Viera", "Veena Man", "Veena Woman", "Veena", "Veena" ),	//	Veena
		],
		[
			//	The First
			null,	//	Invalid Race
			( "Hume Man", "Hume Woman", "Hume", "Hume", "Hume Man", "Hume Woman", "Hume", "Hume" ),	//	Midlander
			( "Hume Man", "Hume Woman", "Hume", "Hume", "Hume Man", "Hume Woman", "Hume", "Hume" ),	//	Highlander
			( "Elven Man", "Elven Woman", "Elf", "Elf", "Elven Man", "Elven Woman", "Elf", "Elf" ),	//	Wildwood
			( "Elven Man", "Elven Woman", "Elf", "Elf", "Elven Man", "Elven Woman", "Elf", "Elf" ),	//	Duskwight
			( "Dwarven Man", "Dwarven Woman", "Dwarf", "Dwarf", "Dwarven Man", "Dwarven Woman", "Dwarf", "Dwarf" ),	//	Plainsfolk
			( "Dwarven Man", "Dwarven Woman", "Dwarf", "Dwarf", "Dwarven Man", "Dwarven Woman", "Dwarf", "Dwarf" ),	//	Dunesfolk
			( "Mystel Man", "Mystel Woman", "Mystel", "Mystel", "Mystel Man", "Mystel Woman", "Mystel", "Mystel" ),	//	Seekers
			( "Mystel Man", "Mystel Woman", "Mystel", "Mystel", "Mystel Man", "Mystel Woman", "Mystel", "Mystel" ),	//	Keepers
			( "Galdjent Man", "Galdjent Woman", "Galdjent", "Galdjent", "Galdjent Man", "Galdjent Woman", "Galdjent", "Galdjent" ),	//	Sea Wolf
			( "Galdjent Man", "Galdjent Woman", "Galdjent", "Galdjent", "Galdjent Man", "Galdjent Woman", "Galdjent", "Galdjent" ),	//	Hellsguard
			( "Drahn Man", "Drahn Woman", "Drahn", "Drahn", "Drahn Man", "Drahn Woman", "Drahn", "Drahn" ),	//	Raen
			( "Drahn Man", "Drahn Woman", "Drahn", "Drahn", "Drahn Man", "Drahn Woman", "Drahn", "Drahn" ),	//	Xaela
			( "Ronso Man", "Ronso Woman", "Ronso", "Ronso", "Ronso Man", "Ronso Woman", "Ronso", "Ronso" ),	//	Helion
			( "Ronso Man", "Ronso Woman", "Ronso", "Ronso", "Ronso Man", "Ronso Woman", "Ronso", "Ronso" ),	//	The Lost
			( "Viis Man", "Viis Woman", "Viis", "Viis", "Rava Man", "Rava Woman", "Rava", "Rava" ),	//	Rava
			( "Viis Man", "Viis Woman", "Viis", "Viis", "Veena Man", "Veena Woman", "Veena", "Veena" ),	//	Veena
		],
		[
			//	Tural
			null,	//	Invalid Race
			( "Tonawawtan Man", "Tonawawtan Woman", "Tonawawta", "Tonawawta", "Tonawawtan Man", "Tonawawtan Woman", "Tonawawta", "Tonawawta" ),	//	Midlander
			( "Tonawawtan Man", "Tonawawtan Woman", "Tonawawta", "Tonawawta", "Tonawawtan Man", "Tonawawtan Woman", "Tonawawta", "Tonawawta" ),	//	Highlander
			( "Elezen Man", "Elezen Woman", "Elezen", "Elezen", "Wildwood Man", "Wildwood Woman", "Wildwood", "Wildwood" ),	//	Wildwood
			( "Elezen Man", "Elezen Woman", "Elezen", "Elezen", "Duskwight Man", "Duskwight Woman", "Duskwight", "Duskwight" ),	//	Duskwight
			( "Lalafellin Man", "Lalafellin Woman", "Lalafell", "Lalafell", "Plainsfolk Man", "Plainsfolk Woman", "Plainsfolk", "Plainsfolk" ),	//	Plainsfolk
			( "Lalafellin Man", "Lalafellin Woman", "Lalafell", "Lalafell", "Dunesfolk Man", "Dunesfolk Woman", "Dunesfolk", "Dunesfolk" ),	//	Dunesfolk
			( "Hhetsarro Man", "Hhetsarro Woman", "Hhetsarro", "Hhetsarro", "Hhetsarro Man", "Hhetsarro Woman", "Hhetsarro", "Hhetsarro" ),	//	Seekers
			( "Hhetsarro Man", "Hhetsarro Woman", "Hhetsarro", "Hhetsarro", "Hhetsarro Man", "Hhetsarro Woman", "Hhetsarro", "Hhetsarro" ),	//	Keepers
			( "Roegadyn Man", "Roegadyn Woman", "Roegadyn", "Roegadyn", "Male Sea Wolf", "Female Sea Wolf", "Sea Wolf", "Sea Wolf" ),	//	Sea Wolf
			( "Roegadyn Man", "Roegadyn Woman", "Roegadyn", "Roegadyn", "Hellsguard Man", "Hellsguard Woman", "Hellsguard", "Hellsguard" ),	//	Hellsguard
			( "Au Ra Man", "Au Ra Woman", "Au Ra", "Au Ra", "Raen Man", "Raen Woman", "Raen", "Raen" ),	//	Raen
			( "Au Ra Man", "Au Ra Woman", "Au Ra", "Au Ra", "Xaela Man", "Xaela Woman", "Xaela", "Xaela" ),	//	Xaela
			( "Xbr'aal Man", "Xbr'aal Woman", "Xbr'aal", "Xbr'aal", "Xbr'aal Man", "Xbr'aal Woman", "Xbr'aal", "Xbr'aal" ),	//	Helion
			( "Xbr'aal Man", "Xbr'aal Woman", "Xbr'aal", "Xbr'aal", "Xbr'aal Man", "Xbr'aal Woman", "Xbr'aal", "Xbr'aal" ),	//	The Lost
			( "Shetona Man", "Shetona Woman", "Shetona", "Shetona", "Shetona Man", "Shetona Woman", "Shetona", "Shetona" ),	//	Rava
			( "Shetona Man", "Shetona Woman", "Shetona", "Shetona", "Shetona Man", "Shetona Woman", "Shetona", "Shetona" ),	//	Veena
		],
		[
			//	The Ninth
			null,	//	Invalid Race
			( "Hyune Man", "Hyune Woman", "Hyune", "Hyune", "Hyune Man", "Hyune Woman", "Hyune", "Hyune" ),	//	Midlander
			( "Hyune Man", "Hyune Woman", "Hyune", "Hyune", "Hyune Man", "Hyune Woman", "Hyune", "Hyune" ),	//	Highlander
			( "Eldite Man", "Eldite Woman", "Eldite", "Eldite", "Eldite Man", "Eldite Woman", "Eldite", "Eldite" ),	//	Wildwood
			( "Eldite Man", "Eldite Woman", "Eldite", "Eldite", "Eldite Man", "Eldite Woman", "Eldite", "Eldite" ),	//	Duskwight
			( "Male Milalla", "Female Milalla", "Milalla", "Milalla", "Male Milalla", "Female Milalla", "Milalla", "Milalla" ),	//	Plainsfolk
			( "Male Milalla", "Female Milalla", "Milalla", "Milalla", "Male Milalla", "Female Milalla", "Milalla", "Milalla" ),	//	Dunesfolk
			( "Hhetsarro Man", "Hhetsarro Woman", "Hhetsarro", "Hhetsarro", "Male Hhetsarro", "Female Hhetsarro", "Hhetsarro", "Hhetsarro" ),	//	Seekers
			( "Hhetsarro Man", "Hhetsarro Woman", "Hhetsarro", "Hhetsarro", "Male Hhetsarro", "Female Hhetsarro", "Hhetsarro", "Hhetsarro" ),	//	Keepers
			( "Roegadyn Man", "Roegadyn Woman", "Roegadyn", "Roegadyn", "Male Sea Wolf", "Female Sea Wolf", "Sea Wolf", "Sea Wolf" ),	//	Sea Wolf
			( "Roegadyn Man", "Roegadyn Woman", "Roegadyn", "Roegadyn", "Hellsguard Man", "Hellsguard Woman", "Hellsguard", "Hellsguard" ),	//	Hellsguard
			( "Au Ra Man", "Au Ra Woman", "Au Ra", "Au Ra", "Raen Man", "Raen Woman", "Raen", "Raen" ),	//	Raen
			( "Au Ra Man", "Au Ra Woman", "Au Ra", "Au Ra", "Xaela Man", "Xaela Woman", "Xaela", "Xaela" ),	//	Xaela
			( "Xbr'aal Man", "Xbr'aal Woman", "Xbr'aal", "Xbr'aal", "Xbr'aal Man", "Xbr'aal Woman", "Xbr'aal", "Xbr'aal" ),	//	Helion
			( "Xbr'aal Man", "Xbr'aal Woman", "Xbr'aal", "Xbr'aal", "Xbr'aal Man", "Xbr'aal Woman", "Xbr'aal", "Xbr'aal" ),	//	The Lost
			( "Shetona Man", "Shetona Woman", "Shetona", "Shetona", "Shetona Man", "Shetona Woman", "Shetona", "Shetona" ),	//	Rava
			( "Shetona Man", "Shetona Woman", "Shetona", "Shetona", "Shetona Man", "Shetona Woman", "Shetona", "Shetona" ),	//	Veena
		],
	];

	//***** TODO
	private static readonly (string race_Masculine, string race_Feminine, string race_MasculineReduced, string race_FeminineReduced, string tribe_Masculine, string tribe_Feminine, string tribe_MasculineReduced, string tribe_FeminineReduced)?[][]
	mNpcNames_De =
	[
		[
			//	Default
			null,	//	Invalid Race
			( "Hyuran Man", "Hyuran Woman", "Hyur", "Hyur", "Midlander Man", "Midlander Woman", "Midlander", "Midlander"),	//	Midlander
			( "Hyuran Man", "Hyuran Woman", "Hyur", "Hyur", "Highlander Man", "Highlander Woman", "Highlander", "Highlander" ),	//	Highlander
			( "Elezen Man", "Elezen Woman", "Elezen", "Elezen", "Wildwood Man", "Wildwood Woman", "Wildwood", "Wildwood" ),	//	Wildwood
			( "Elezen Man", "Elezen Woman", "Elezen", "Elezen", "Duskwight Man", "Duskwight Woman", "Duskwight", "Duskwight" ),	//	Duskwight
			( "Lalafellin Man", "Lalafellin Woman", "Lalafell", "Lalafell", "Plainsfolk Man", "Plainsfolk Woman", "Plainsfolk", "Plainsfolk" ),	//	Plainsfolk
			( "Lalafellin Man", "Lalafellin Woman", "Lalafell", "Lalafell", "Dunesfolk Man", "Dunesfolk Woman", "Dunesfolk", "Dunesfolk" ),	//	Dunesfolk
			( "Miqo'te Man", "Miqo'te Woman", "Miqo'te", "Miqo'te", "Male Seeker", "Female Seeker", "Seeker of the Sun", "Seeker of the Sun" ),	//	Seekers
			( "Miqo'te Man", "Miqo'te Woman", "Miqo'te", "Miqo'te", "Male Keeper", "Female Keeper", "Keeper of the Moon", "Keeper of the Moon" ),	//	Keepers
			( "Roegadyn Man", "Roegadyn Woman", "Roegadyn", "Roegadyn", "Male Sea Wolf", "Female Sea Wolf", "Sea Wolf", "Sea Wolf" ),	//	Sea Wolf
			( "Roegadyn Man", "Roegadyn Woman", "Roegadyn", "Roegadyn", "Hellsguard Man", "Hellsguard Woman", "Hellsguard", "Hellsguard" ),	//	Hellsguard
			( "Au Ra Man", "Au Ra Woman", "Au Ra", "Au Ra", "Raen Man", "Raen Woman", "Raen", "Raen" ),	//	Raen
			( "Au Ra Man", "Au Ra Woman", "Au Ra", "Au Ra", "Xaela Man", "Xaela Woman", "Xaela", "Xaela" ),	//	Xaela
			( "Hrothgar Man", "Hrothgar Woman", "Hrothgar", "Hrothgar", "Male Helion", "Female Helion", "Helion", "Helion" ),	//	Helion
			( "Hrothgar Man", "Hrothgar Woman", "Hrothgar", "Hrothgar", "Lost Man", "Lost Woman", "Lost", "Lost" ),	//	The Lost
			( "Viera Man", "Viera Woman", "Viera", "Viera", "Rava Man", "Rava Woman", "Rava", "Rava" ),	//	Rava
			( "Viera Man", "Viera Woman", "Viera", "Viera", "Veena Man", "Veena Woman", "Veena", "Veena" ),	//	Veena
		],
		[
			//	The First
			null,	//	Invalid Race
			( "Hume Man", "Hume Woman", "Hume", "Hume", "Hume Man", "Hume Woman", "Hume", "Hume" ),	//	Midlander
			( "Hume Man", "Hume Woman", "Hume", "Hume", "Hume Man", "Hume Woman", "Hume", "Hume" ),	//	Highlander
			( "Elven Man", "Elven Woman", "Elf", "Elf", "Elven Man", "Elven Woman", "Elf", "Elf" ),	//	Wildwood
			( "Elven Man", "Elven Woman", "Elf", "Elf", "Elven Man", "Elven Woman", "Elf", "Elf" ),	//	Duskwight
			( "Dwarven Man", "Dwarven Woman", "Dwarf", "Dwarf", "Dwarven Man", "Dwarven Woman", "Dwarf", "Dwarf" ),	//	Plainsfolk
			( "Dwarven Man", "Dwarven Woman", "Dwarf", "Dwarf", "Dwarven Man", "Dwarven Woman", "Dwarf", "Dwarf" ),	//	Dunesfolk
			( "Mystel Man", "Mystel Woman", "Mystel", "Mystel", "Mystel Man", "Mystel Woman", "Mystel", "Mystel" ),	//	Seekers
			( "Mystel Man", "Mystel Woman", "Mystel", "Mystel", "Mystel Man", "Mystel Woman", "Mystel", "Mystel" ),	//	Keepers
			( "Galdjent Man", "Galdjent Woman", "Galdjent", "Galdjent", "Galdjent Man", "Galdjent Woman", "Galdjent", "Galdjent" ),	//	Sea Wolf
			( "Galdjent Man", "Galdjent Woman", "Galdjent", "Galdjent", "Galdjent Man", "Galdjent Woman", "Galdjent", "Galdjent" ),	//	Hellsguard
			( "Drahn Man", "Drahn Woman", "Drahn", "Drahn", "Drahn Man", "Drahn Woman", "Drahn", "Drahn" ),	//	Raen
			( "Drahn Man", "Drahn Woman", "Drahn", "Drahn", "Drahn Man", "Drahn Woman", "Drahn", "Drahn" ),	//	Xaela
			( "Ronso Man", "Ronso Woman", "Ronso", "Ronso", "Ronso Man", "Ronso Woman", "Ronso", "Ronso" ),	//	Helion
			( "Ronso Man", "Ronso Woman", "Ronso", "Ronso", "Ronso Man", "Ronso Woman", "Ronso", "Ronso" ),	//	The Lost
			( "Viis Man", "Viis Woman", "Viis", "Viis", "Rava Man", "Rava Woman", "Rava", "Rava" ),	//	Rava
			( "Viis Man", "Viis Woman", "Viis", "Viis", "Veena Man", "Veena Woman", "Veena", "Veena" ),	//	Veena
		],
		[
			//	Tural
			null,	//	Invalid Race
			( "Tonawawtan Man", "Tonawawtan Woman", "Tonawawta", "Tonawawta", "Tonawawtan Man", "Tonawawtan Woman", "Tonawawta", "Tonawawta" ),	//	Midlander
			( "Tonawawtan Man", "Tonawawtan Woman", "Tonawawta", "Tonawawta", "Tonawawtan Man", "Tonawawtan Woman", "Tonawawta", "Tonawawta" ),	//	Highlander
			( "Elezen Man", "Elezen Woman", "Elezen", "Elezen", "Wildwood Man", "Wildwood Woman", "Wildwood", "Wildwood" ),	//	Wildwood
			( "Elezen Man", "Elezen Woman", "Elezen", "Elezen", "Duskwight Man", "Duskwight Woman", "Duskwight", "Duskwight" ),	//	Duskwight
			( "Lalafellin Man", "Lalafellin Woman", "Lalafell", "Lalafell", "Plainsfolk Man", "Plainsfolk Woman", "Plainsfolk", "Plainsfolk" ),	//	Plainsfolk
			( "Lalafellin Man", "Lalafellin Woman", "Lalafell", "Lalafell", "Dunesfolk Man", "Dunesfolk Woman", "Dunesfolk", "Dunesfolk" ),	//	Dunesfolk
			( "Hhetsarro Man", "Hhetsarro Woman", "Hhetsarro", "Hhetsarro", "Hhetsarro Man", "Hhetsarro Woman", "Hhetsarro", "Hhetsarro" ),	//	Seekers
			( "Hhetsarro Man", "Hhetsarro Woman", "Hhetsarro", "Hhetsarro", "Hhetsarro Man", "Hhetsarro Woman", "Hhetsarro", "Hhetsarro" ),	//	Keepers
			( "Roegadyn Man", "Roegadyn Woman", "Roegadyn", "Roegadyn", "Male Sea Wolf", "Female Sea Wolf", "Sea Wolf", "Sea Wolf" ),	//	Sea Wolf
			( "Roegadyn Man", "Roegadyn Woman", "Roegadyn", "Roegadyn", "Hellsguard Man", "Hellsguard Woman", "Hellsguard", "Hellsguard" ),	//	Hellsguard
			( "Au Ra Man", "Au Ra Woman", "Au Ra", "Au Ra", "Raen Man", "Raen Woman", "Raen", "Raen" ),	//	Raen
			( "Au Ra Man", "Au Ra Woman", "Au Ra", "Au Ra", "Xaela Man", "Xaela Woman", "Xaela", "Xaela" ),	//	Xaela
			( "Xbr'aal Man", "Xbr'aal Woman", "Xbr'aal", "Xbr'aal", "Xbr'aal Man", "Xbr'aal Woman", "Xbr'aal", "Xbr'aal" ),	//	Helion
			( "Xbr'aal Man", "Xbr'aal Woman", "Xbr'aal", "Xbr'aal", "Xbr'aal Man", "Xbr'aal Woman", "Xbr'aal", "Xbr'aal" ),	//	The Lost
			( "Shetona Man", "Shetona Woman", "Shetona", "Shetona", "Shetona Man", "Shetona Woman", "Shetona", "Shetona" ),	//	Rava
			( "Shetona Man", "Shetona Woman", "Shetona", "Shetona", "Shetona Man", "Shetona Woman", "Shetona", "Shetona" ),	//	Veena
		],
		[
			//	The Ninth
			null,	//	Invalid Race
			( "Hyune Man", "Hyune Woman", "Hyune", "Hyune", "Hyune Man", "Hyune Woman", "Hyune", "Hyune" ),	//	Midlander
			( "Hyune Man", "Hyune Woman", "Hyune", "Hyune", "Hyune Man", "Hyune Woman", "Hyune", "Hyune" ),	//	Highlander
			( "Eldite Man", "Eldite Woman", "Eldite", "Eldite", "Eldite Man", "Eldite Woman", "Eldite", "Eldite" ),	//	Wildwood
			( "Eldite Man", "Eldite Woman", "Eldite", "Eldite", "Eldite Man", "Eldite Woman", "Eldite", "Eldite" ),	//	Duskwight
			( "Male Milalla", "Female Milalla", "Milalla", "Milalla", "Male Milalla", "Female Milalla", "Milalla", "Milalla" ),	//	Plainsfolk
			( "Male Milalla", "Female Milalla", "Milalla", "Milalla", "Male Milalla", "Female Milalla", "Milalla", "Milalla" ),	//	Dunesfolk
			( "Hhetsarro Man", "Hhetsarro Woman", "Hhetsarro", "Hhetsarro", "Male Hhetsarro", "Female Hhetsarro", "Hhetsarro", "Hhetsarro" ),	//	Seekers
			( "Hhetsarro Man", "Hhetsarro Woman", "Hhetsarro", "Hhetsarro", "Male Hhetsarro", "Female Hhetsarro", "Hhetsarro", "Hhetsarro" ),	//	Keepers
			( "Roegadyn Man", "Roegadyn Woman", "Roegadyn", "Roegadyn", "Male Sea Wolf", "Female Sea Wolf", "Sea Wolf", "Sea Wolf" ),	//	Sea Wolf
			( "Roegadyn Man", "Roegadyn Woman", "Roegadyn", "Roegadyn", "Hellsguard Man", "Hellsguard Woman", "Hellsguard", "Hellsguard" ),	//	Hellsguard
			( "Au Ra Man", "Au Ra Woman", "Au Ra", "Au Ra", "Raen Man", "Raen Woman", "Raen", "Raen" ),	//	Raen
			( "Au Ra Man", "Au Ra Woman", "Au Ra", "Au Ra", "Xaela Man", "Xaela Woman", "Xaela", "Xaela" ),	//	Xaela
			( "Xbr'aal Man", "Xbr'aal Woman", "Xbr'aal", "Xbr'aal", "Xbr'aal Man", "Xbr'aal Woman", "Xbr'aal", "Xbr'aal" ),	//	Helion
			( "Xbr'aal Man", "Xbr'aal Woman", "Xbr'aal", "Xbr'aal", "Xbr'aal Man", "Xbr'aal Woman", "Xbr'aal", "Xbr'aal" ),	//	The Lost
			( "Shetona Man", "Shetona Woman", "Shetona", "Shetona", "Shetona Man", "Shetona Woman", "Shetona", "Shetona" ),	//	Rava
			( "Shetona Man", "Shetona Woman", "Shetona", "Shetona", "Shetona Man", "Shetona Woman", "Shetona", "Shetona" ),	//	Veena
		],
	];

	//***** TODO
	private static readonly (string race_Masculine, string race_Feminine, string race_MasculineReduced, string race_FeminineReduced, string tribe_Masculine, string tribe_Feminine, string tribe_MasculineReduced, string tribe_FeminineReduced)?[][]
	mNpcNames_Fr =
	[
		[
			//	Default
			null,	//	Invalid Race
			( "Hyuran Man", "Hyuran Woman", "Hyur", "Hyur", "Midlander Man", "Midlander Woman", "Midlander", "Midlander"),	//	Midlander
			( "Hyuran Man", "Hyuran Woman", "Hyur", "Hyur", "Highlander Man", "Highlander Woman", "Highlander", "Highlander" ),	//	Highlander
			( "Elezen Man", "Elezen Woman", "Elezen", "Elezen", "Wildwood Man", "Wildwood Woman", "Wildwood", "Wildwood" ),	//	Wildwood
			( "Elezen Man", "Elezen Woman", "Elezen", "Elezen", "Duskwight Man", "Duskwight Woman", "Duskwight", "Duskwight" ),	//	Duskwight
			( "Lalafellin Man", "Lalafellin Woman", "Lalafell", "Lalafell", "Plainsfolk Man", "Plainsfolk Woman", "Plainsfolk", "Plainsfolk" ),	//	Plainsfolk
			( "Lalafellin Man", "Lalafellin Woman", "Lalafell", "Lalafell", "Dunesfolk Man", "Dunesfolk Woman", "Dunesfolk", "Dunesfolk" ),	//	Dunesfolk
			( "Miqo'te Man", "Miqo'te Woman", "Miqo'te", "Miqo'te", "Male Seeker", "Female Seeker", "Seeker of the Sun", "Seeker of the Sun" ),	//	Seekers
			( "Miqo'te Man", "Miqo'te Woman", "Miqo'te", "Miqo'te", "Male Keeper", "Female Keeper", "Keeper of the Moon", "Keeper of the Moon" ),	//	Keepers
			( "Roegadyn Man", "Roegadyn Woman", "Roegadyn", "Roegadyn", "Male Sea Wolf", "Female Sea Wolf", "Sea Wolf", "Sea Wolf" ),	//	Sea Wolf
			( "Roegadyn Man", "Roegadyn Woman", "Roegadyn", "Roegadyn", "Hellsguard Man", "Hellsguard Woman", "Hellsguard", "Hellsguard" ),	//	Hellsguard
			( "Au Ra Man", "Au Ra Woman", "Au Ra", "Au Ra", "Raen Man", "Raen Woman", "Raen", "Raen" ),	//	Raen
			( "Au Ra Man", "Au Ra Woman", "Au Ra", "Au Ra", "Xaela Man", "Xaela Woman", "Xaela", "Xaela" ),	//	Xaela
			( "Hrothgar Man", "Hrothgar Woman", "Hrothgar", "Hrothgar", "Male Helion", "Female Helion", "Helion", "Helion" ),	//	Helion
			( "Hrothgar Man", "Hrothgar Woman", "Hrothgar", "Hrothgar", "Lost Man", "Lost Woman", "Lost", "Lost" ),	//	The Lost
			( "Viera Man", "Viera Woman", "Viera", "Viera", "Rava Man", "Rava Woman", "Rava", "Rava" ),	//	Rava
			( "Viera Man", "Viera Woman", "Viera", "Viera", "Veena Man", "Veena Woman", "Veena", "Veena" ),	//	Veena
		],
		[
			//	The First
			null,	//	Invalid Race
			( "Hume Man", "Hume Woman", "Hume", "Hume", "Hume Man", "Hume Woman", "Hume", "Hume" ),	//	Midlander
			( "Hume Man", "Hume Woman", "Hume", "Hume", "Hume Man", "Hume Woman", "Hume", "Hume" ),	//	Highlander
			( "Elven Man", "Elven Woman", "Elf", "Elf", "Elven Man", "Elven Woman", "Elf", "Elf" ),	//	Wildwood
			( "Elven Man", "Elven Woman", "Elf", "Elf", "Elven Man", "Elven Woman", "Elf", "Elf" ),	//	Duskwight
			( "Dwarven Man", "Dwarven Woman", "Dwarf", "Dwarf", "Dwarven Man", "Dwarven Woman", "Dwarf", "Dwarf" ),	//	Plainsfolk
			( "Dwarven Man", "Dwarven Woman", "Dwarf", "Dwarf", "Dwarven Man", "Dwarven Woman", "Dwarf", "Dwarf" ),	//	Dunesfolk
			( "Mystel Man", "Mystel Woman", "Mystel", "Mystel", "Mystel Man", "Mystel Woman", "Mystel", "Mystel" ),	//	Seekers
			( "Mystel Man", "Mystel Woman", "Mystel", "Mystel", "Mystel Man", "Mystel Woman", "Mystel", "Mystel" ),	//	Keepers
			( "Galdjent Man", "Galdjent Woman", "Galdjent", "Galdjent", "Galdjent Man", "Galdjent Woman", "Galdjent", "Galdjent" ),	//	Sea Wolf
			( "Galdjent Man", "Galdjent Woman", "Galdjent", "Galdjent", "Galdjent Man", "Galdjent Woman", "Galdjent", "Galdjent" ),	//	Hellsguard
			( "Drahn Man", "Drahn Woman", "Drahn", "Drahn", "Drahn Man", "Drahn Woman", "Drahn", "Drahn" ),	//	Raen
			( "Drahn Man", "Drahn Woman", "Drahn", "Drahn", "Drahn Man", "Drahn Woman", "Drahn", "Drahn" ),	//	Xaela
			( "Ronso Man", "Ronso Woman", "Ronso", "Ronso", "Ronso Man", "Ronso Woman", "Ronso", "Ronso" ),	//	Helion
			( "Ronso Man", "Ronso Woman", "Ronso", "Ronso", "Ronso Man", "Ronso Woman", "Ronso", "Ronso" ),	//	The Lost
			( "Viis Man", "Viis Woman", "Viis", "Viis", "Rava Man", "Rava Woman", "Rava", "Rava" ),	//	Rava
			( "Viis Man", "Viis Woman", "Viis", "Viis", "Veena Man", "Veena Woman", "Veena", "Veena" ),	//	Veena
		],
		[
			//	Tural
			null,	//	Invalid Race
			( "Tonawawtan Man", "Tonawawtan Woman", "Tonawawta", "Tonawawta", "Tonawawtan Man", "Tonawawtan Woman", "Tonawawta", "Tonawawta" ),	//	Midlander
			( "Tonawawtan Man", "Tonawawtan Woman", "Tonawawta", "Tonawawta", "Tonawawtan Man", "Tonawawtan Woman", "Tonawawta", "Tonawawta" ),	//	Highlander
			( "Elezen Man", "Elezen Woman", "Elezen", "Elezen", "Wildwood Man", "Wildwood Woman", "Wildwood", "Wildwood" ),	//	Wildwood
			( "Elezen Man", "Elezen Woman", "Elezen", "Elezen", "Duskwight Man", "Duskwight Woman", "Duskwight", "Duskwight" ),	//	Duskwight
			( "Lalafellin Man", "Lalafellin Woman", "Lalafell", "Lalafell", "Plainsfolk Man", "Plainsfolk Woman", "Plainsfolk", "Plainsfolk" ),	//	Plainsfolk
			( "Lalafellin Man", "Lalafellin Woman", "Lalafell", "Lalafell", "Dunesfolk Man", "Dunesfolk Woman", "Dunesfolk", "Dunesfolk" ),	//	Dunesfolk
			( "Hhetsarro Man", "Hhetsarro Woman", "Hhetsarro", "Hhetsarro", "Hhetsarro Man", "Hhetsarro Woman", "Hhetsarro", "Hhetsarro" ),	//	Seekers
			( "Hhetsarro Man", "Hhetsarro Woman", "Hhetsarro", "Hhetsarro", "Hhetsarro Man", "Hhetsarro Woman", "Hhetsarro", "Hhetsarro" ),	//	Keepers
			( "Roegadyn Man", "Roegadyn Woman", "Roegadyn", "Roegadyn", "Male Sea Wolf", "Female Sea Wolf", "Sea Wolf", "Sea Wolf" ),	//	Sea Wolf
			( "Roegadyn Man", "Roegadyn Woman", "Roegadyn", "Roegadyn", "Hellsguard Man", "Hellsguard Woman", "Hellsguard", "Hellsguard" ),	//	Hellsguard
			( "Au Ra Man", "Au Ra Woman", "Au Ra", "Au Ra", "Raen Man", "Raen Woman", "Raen", "Raen" ),	//	Raen
			( "Au Ra Man", "Au Ra Woman", "Au Ra", "Au Ra", "Xaela Man", "Xaela Woman", "Xaela", "Xaela" ),	//	Xaela
			( "Xbr'aal Man", "Xbr'aal Woman", "Xbr'aal", "Xbr'aal", "Xbr'aal Man", "Xbr'aal Woman", "Xbr'aal", "Xbr'aal" ),	//	Helion
			( "Xbr'aal Man", "Xbr'aal Woman", "Xbr'aal", "Xbr'aal", "Xbr'aal Man", "Xbr'aal Woman", "Xbr'aal", "Xbr'aal" ),	//	The Lost
			( "Shetona Man", "Shetona Woman", "Shetona", "Shetona", "Shetona Man", "Shetona Woman", "Shetona", "Shetona" ),	//	Rava
			( "Shetona Man", "Shetona Woman", "Shetona", "Shetona", "Shetona Man", "Shetona Woman", "Shetona", "Shetona" ),	//	Veena
		],
		[
			//	The Ninth
			null,	//	Invalid Race
			( "Hyune Man", "Hyune Woman", "Hyune", "Hyune", "Hyune Man", "Hyune Woman", "Hyune", "Hyune" ),	//	Midlander
			( "Hyune Man", "Hyune Woman", "Hyune", "Hyune", "Hyune Man", "Hyune Woman", "Hyune", "Hyune" ),	//	Highlander
			( "Eldite Man", "Eldite Woman", "Eldite", "Eldite", "Eldite Man", "Eldite Woman", "Eldite", "Eldite" ),	//	Wildwood
			( "Eldite Man", "Eldite Woman", "Eldite", "Eldite", "Eldite Man", "Eldite Woman", "Eldite", "Eldite" ),	//	Duskwight
			( "Male Milalla", "Female Milalla", "Milalla", "Milalla", "Male Milalla", "Female Milalla", "Milalla", "Milalla" ),	//	Plainsfolk
			( "Male Milalla", "Female Milalla", "Milalla", "Milalla", "Male Milalla", "Female Milalla", "Milalla", "Milalla" ),	//	Dunesfolk
			( "Hhetsarro Man", "Hhetsarro Woman", "Hhetsarro", "Hhetsarro", "Male Hhetsarro", "Female Hhetsarro", "Hhetsarro", "Hhetsarro" ),	//	Seekers
			( "Hhetsarro Man", "Hhetsarro Woman", "Hhetsarro", "Hhetsarro", "Male Hhetsarro", "Female Hhetsarro", "Hhetsarro", "Hhetsarro" ),	//	Keepers
			( "Roegadyn Man", "Roegadyn Woman", "Roegadyn", "Roegadyn", "Male Sea Wolf", "Female Sea Wolf", "Sea Wolf", "Sea Wolf" ),	//	Sea Wolf
			( "Roegadyn Man", "Roegadyn Woman", "Roegadyn", "Roegadyn", "Hellsguard Man", "Hellsguard Woman", "Hellsguard", "Hellsguard" ),	//	Hellsguard
			( "Au Ra Man", "Au Ra Woman", "Au Ra", "Au Ra", "Raen Man", "Raen Woman", "Raen", "Raen" ),	//	Raen
			( "Au Ra Man", "Au Ra Woman", "Au Ra", "Au Ra", "Xaela Man", "Xaela Woman", "Xaela", "Xaela" ),	//	Xaela
			( "Xbr'aal Man", "Xbr'aal Woman", "Xbr'aal", "Xbr'aal", "Xbr'aal Man", "Xbr'aal Woman", "Xbr'aal", "Xbr'aal" ),	//	Helion
			( "Xbr'aal Man", "Xbr'aal Woman", "Xbr'aal", "Xbr'aal", "Xbr'aal Man", "Xbr'aal Woman", "Xbr'aal", "Xbr'aal" ),	//	The Lost
			( "Shetona Man", "Shetona Woman", "Shetona", "Shetona", "Shetona Man", "Shetona Woman", "Shetona", "Shetona" ),	//	Rava
			( "Shetona Man", "Shetona Woman", "Shetona", "Shetona", "Shetona Man", "Shetona Woman", "Shetona", "Shetona" ),	//	Veena
		],
	];
}
