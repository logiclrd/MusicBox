using System;
using System.Collections.Generic;
using System.Linq;

namespace MusicBox
{
	class SearchResultEventSink
	{
		public WeakReference<SearchResult> WeakThis;
		public FileReference FileReference;

		public SearchResultEventSink(SearchResult @this, FileReference fileReference)
		{
			this.WeakThis = new WeakReference<SearchResult>(@this);
			this.FileReference = fileReference;
		}

		public static void Attach(SearchResult @this, FileReference fileReference)
		{
			var sink = new SearchResultEventSink(@this, fileReference);

			fileReference.SortKeyChanged += sink.SortKeyChanged;
		}

		void SortKeyChanged(object sender, SortKeyChangedEventArgs e)
		{
			if (!WeakThis.TryGetTarget(out var @this))
				this.FileReference.SortKeyChanged -= this.SortKeyChanged;
			else
				@this.RecomputeComponents();
		}
	}

	public class SearchResult
	{
		public SearchResult(FileReference fileReference, string highlightSubstring)
		{
			this.FileReference = fileReference;
			this.HighlightSubstring = highlightSubstring;

			WeakReference<SearchResult> weakThis = new WeakReference<SearchResult>(this);

			SearchResultEventSink.Attach(this, fileReference);
		}

		public readonly FileReference FileReference;
		public readonly string HighlightSubstring;

		public List<SearchResultComponent> Components = new List<SearchResultComponent>();

		public event EventHandler ComponentsChanged;

		static readonly char[] PathSeparatorChars = { '/', '\\' };

		public void RecomputeComponents()
		{
			this.Components.Clear();

			string renderText = this.FileReference.SortKey;

			int offset = (HighlightSubstring.Length == 0) ? -1 : renderText.IndexOf(HighlightSubstring, StringComparison.OrdinalIgnoreCase);

			while (offset >= 0)
			{
				AddSegment(renderText.Substring(0, offset), highlight: false);
				AddSegment(renderText.Substring(offset, HighlightSubstring.Length), highlight: true);

				renderText = renderText.Substring(offset + HighlightSubstring.Length);
				offset = renderText.IndexOf(HighlightSubstring, StringComparison.OrdinalIgnoreCase);
			}

			if (renderText.Length > 0)
				AddSegment(renderText, highlight: false);

			ComponentsChanged?.Invoke(this, EventArgs.Empty);
		}

		void AddSegment(string renderText, bool highlight)
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
