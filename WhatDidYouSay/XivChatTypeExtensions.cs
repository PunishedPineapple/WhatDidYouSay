using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Dalamud.Game.Text;

namespace WhatDidYouSay;

internal static class XivChatTypeExtensions
{
    extension( XivChatType chatType )
    {
		public XivChatType Channel => (XivChatType)( (ushort)chatType & 0x7F );
		public XivChatTargetMask TargetMask => (XivChatTargetMask)( (ushort)chatType & 0x780 );
        public XivChatSourceMask SourceMask => (XivChatSourceMask)( (ushort)chatType & 0x7800 );
	}
}

// Target masks start at 0x80 and go as high as 0x780 (bits 7-10).
#region Target Masks

internal enum XivChatTargetMask : ushort
{
    /// <summary>
    /// The target mask for the player.
    /// </summary>
    You = 0x80,

    /// <summary>
    /// The target mask for party members.
    /// </summary>
    PartyMember = 0x100,

    /// <summary>
    /// The target mask for alliance members.
    /// </summary>
    AllianceMember = 0x180,

    /// <summary>
    /// The target mask for other players.
    /// </summary>
    OtherPlayers = 0x200,

    /// <summary>
    /// The target mask for engaged enemies.
    /// </summary>
    EngagedEnemy = 0x280,

    /// <summary>
    /// The target mask for unengaged enemies.
    /// </summary>
    UnengagedEnemy = 0x300,

    /// <summary>
    /// The target mask for friendly NPCs.
    /// </summary>
    FriendlyNPC = 0x380,

    /// <summary>
    /// The target mask for the player's pet.
    /// </summary>
    YourPet = 0x400,

    /// <summary>
    /// The target mask for party members' pets.
    /// </summary>
    PartyPet = 0x480,

    /// <summary>
    /// The target mask for alliance members' pets.
    /// </summary>
    AlliancePet = 0x500,

    /// <summary>
    /// The target mask for other players' pets.
    /// </summary>
    OtherPlayerPet = 0x580,
}

#endregion

// Source masks start at 0x800 and go as high as 0x7800 (bits 11-14).
#region Source Masks

internal enum XivChatSourceMask : ushort
{
	/// <summary>
	/// The source mask for the player.
	/// </summary>
	You = 0x800,

    /// <summary>
    /// The source mask for party members.
    /// </summary>
    PartyMember = 0x1000,

    /// <summary>
    /// The source mask for alliance members.
    /// </summary>
    AllianceMember = 0x1800,

    /// <summary>
    /// The source mask for other players.
    /// </summary>
    OtherPlayers = 0x2000,

    /// <summary>
    /// The source mask for engaged enemies.
    /// </summary>
    EngagedEnemy = 0x2800,

    /// <summary>
    /// The source mask for unengaged enemies.
    /// </summary>
    UnengagedEnemy = 0x3000,

    /// <summary>
    /// The source mask for friendly NPCs.
    /// </summary>
    FriendlyNPC = 0x3800,

    /// <summary>
    /// The source mask for the player's pet.
    /// </summary>
    YourPet = 0x4000,

    /// <summary>
    /// The source mask for party members' pets.
    /// </summary>
    PartyPet = 0x4800,

    /// <summary>
    /// The source mask for alliance members' pets.
    /// </summary>
    AlliancePet = 0x5000,

    /// <summary>
    /// The source mask for other players' pets.
    /// </summary>
    OtherPlayerPet = 0x5800,
}

 #endregion
