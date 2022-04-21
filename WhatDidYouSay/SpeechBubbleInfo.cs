using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WhatDidYouSay
{
	internal class SpeechBubbleInfo
	{
		public SpeechBubbleInfo( string messageText, long timeLastSeen_mSec )
		{
			TimeLastSeen_mSec = timeLastSeen_mSec;
			HasBeenPrinted = false;
			MessageText = messageText;
		}
		protected SpeechBubbleInfo(){}

		public long TimeLastSeen_mSec { get; set; }
		public bool HasBeenPrinted { get; set; }
		public string MessageText { get; set; }
	}
}
