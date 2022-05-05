namespace WhatDidYouSay
{
	internal class SpeechBubbleInfo
	{
		public SpeechBubbleInfo( string messageText, long timeLastSeen_mSec, string speakerName = "" )
		{
			TimeLastSeen_mSec = timeLastSeen_mSec;
			HasBeenPrinted = false;
			MessageText = messageText;
			SpeakerName = speakerName;
		}

		protected SpeechBubbleInfo(){}

		public bool IsSameMessageAs( SpeechBubbleInfo rhs )
		{
			return SpeakerName.Equals( rhs.SpeakerName ) && MessageText.Equals( rhs.MessageText );
		}

		public long TimeLastSeen_mSec { get; set; }
		public bool HasBeenPrinted { get; set; }
		public string SpeakerName { get; set; }
		public string MessageText { get; set; }
	}
}
