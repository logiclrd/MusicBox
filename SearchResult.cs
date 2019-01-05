using System;
using System.Collections.Generic;
using System.Linq;

namespace MusicBox
{
	public class SearchResult
	{
		public List<SearchResultComponent> Components = new List<SearchResultComponent>();
		public string RelativePath;
		public string FullPath;

		static readonly char[] PathSeparatorChars = { '/', '\\' };

		public void AddSegment(string renderText, bool highlight)
		{
			int separatorIndex = renderText.IndexOfAny(PathSeparatorChars);

			while (separatorIndex >= 0)
			{
				if (separatorIndex > 0)
				{
					if (Components.Count == 0)
						Components.Add(new SearchResultComponent());

					Components.Last().AddSegment(renderText.Substring(0, separatorIndex), highlight);
				}

				Components.Add(new SearchResultComponent());

				renderText = renderText.Substring(separatorIndex + 1).TrimStart(PathSeparatorChars);
				separatorIndex = renderText.IndexOfAny(PathSeparatorChars);
			}

			if (Components.Count == 0)
				Components.Add(new SearchResultComponent());

			Components.Last().AddSegment(renderText, highlight);
		}
	}
}
