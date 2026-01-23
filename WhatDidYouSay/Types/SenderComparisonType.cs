namespace WhatDidYouSay;

public enum SenderComparisonType : int
{
	ObjectID = 0,
	BaseID = 1,
	NameID_FallbackBaseID = 2,
	NameID_FallbackText = 3,
	NameText = 4,
	NeverEqual = 5,
}