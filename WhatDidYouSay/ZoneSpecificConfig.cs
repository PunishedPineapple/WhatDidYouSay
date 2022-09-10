using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WhatDidYouSay
{
	public class ZoneSpecificConfig
	{
		internal bool mDisableForZone = false;
		internal bool DisableForZone
		{
			get { return mDisableForZone; }
			set { mDisableForZone = value; }
		}

		internal bool mRepeatsAllowed = true;
		internal bool RepeatsAllowed
		{
			get { return mRepeatsAllowed; }
			set { mRepeatsAllowed = value; }
		}

		public int mTimeBeforeRepeatsAllowed_Sec = 10;
		public int TimeBeforeRepeatsAllowed_Sec
		{
			get { return mTimeBeforeRepeatsAllowed_Sec; }
			set { mTimeBeforeRepeatsAllowed_Sec = value; }
		}
	}
}
