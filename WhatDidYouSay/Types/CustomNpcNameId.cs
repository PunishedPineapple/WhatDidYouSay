using System;

namespace WhatDidYouSay;

//	Event NPCs can always be distinguished by their base ID.  Battle NPCs are more complicated.  For now we will just go off of a combination
//	of base ID and name ID (name IDs vary, even when an empty string).  If we run into problems, we may have to discriminate by territory or
//	even more specific things, but for now a base and name is easy and fits into a 64-bit integer for fast lookup.
internal struct CustomNpcNameId : IEquatable<CustomNpcNameId>, IEquatable<UInt64>, IComparable<CustomNpcNameId>, IComparable<UInt64>, IComparable
{
	public CustomNpcNameId( UInt32 baseId, UInt32 nameId )
	{
		BaseId = baseId;
		NameId = nameId;
	}

	private CustomNpcNameId( UInt64 id ) => CombinedId = id;

	public UInt64 CombinedId { get; private set; }

	public UInt32 BaseId
	{
		get => (UInt32)CombinedId;
		set => CombinedId = ( CombinedId & 0xFFFFFFFF00000000 ) | value;
	}

	public UInt32 NameId
	{
		get => (UInt32)( CombinedId >> 32 );
		set => CombinedId = ( CombinedId & 0xFFFFFFFF ) | ( (UInt64)value << 32 );
	}

	public static implicit operator UInt64( CustomNpcNameId customNameId ) => customNameId.CombinedId;
	public static explicit operator CustomNpcNameId( UInt64 customNameId ) => new( customNameId );

	public override string ToString() => $"{BaseId}{( NameId > 0 ? $", {NameId}" : string.Empty )}";

	public readonly bool Equals( CustomNpcNameId rhs ) => CombinedId.Equals( rhs.CombinedId );
	public readonly bool Equals( UInt64 rhs ) => CombinedId.Equals( rhs );
	public readonly override bool Equals( object? rhs ) => rhs is UInt64 or CustomNpcNameId && CombinedId.Equals( (UInt64)rhs );
	public readonly override int GetHashCode() => CombinedId.GetHashCode();
	public readonly int CompareTo( CustomNpcNameId rhs ) => CombinedId.CompareTo( rhs.CombinedId );
	public readonly int CompareTo( UInt64 rhs ) => CombinedId.CompareTo( rhs );
	public readonly int CompareTo( object? rhs ) => rhs is UInt64 or CustomNpcNameId ? CombinedId.CompareTo( (UInt64)rhs ) : -1;
}
