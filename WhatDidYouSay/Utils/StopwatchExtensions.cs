using System.Diagnostics;

namespace WhatDidYouSay;

internal static class StopwatchExtensions
{
	extension( Stopwatch stopwatch )
	{
		public long ElapsedMicroseconds => stopwatch.ElapsedTicks * 1_000_000L / Stopwatch.Frequency;
		public long ElapsedSeconds => stopwatch.ElapsedTicks / Stopwatch.Frequency;
	}
}
