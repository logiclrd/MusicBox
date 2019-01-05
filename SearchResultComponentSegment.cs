namespace MusicBox
{
	public class SearchResultComponentSegment
	{
		public string Text;
		public bool Highlight;

		public SearchResultComponentSegment(string text, bool highlight)
		{
			Text = text;
			Highlight = highlight;
		}
	}
}
