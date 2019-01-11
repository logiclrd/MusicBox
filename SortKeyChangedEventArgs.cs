using System;

namespace MusicBox
{
	public class SortKeyChangedEventArgs : EventArgs
	{
		public string OldSortKey { get; }
		public string NewSortKey { get; }

		public SortKeyChangedEventArgs(string oldSortKey, string newSortKey)
		{
			this.OldSortKey = oldSortKey;
			this.NewSortKey = newSortKey;
		}
	}

	public delegate void SortKeyChangedEventHandler(object sender, SortKeyChangedEventArgs e);
}
