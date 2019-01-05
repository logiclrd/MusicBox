using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace MusicBox
{
	public class SearchResultComponent
	{
		public string Name;
		public List<SearchResultComponentSegment> Segments = new List<SearchResultComponentSegment>();

		public void AddSegment(string text, bool highlight)
		{
			Segments.Add(new SearchResultComponentSegment(text, highlight));

			if (Name == null)
				Name = text;
			else
				Name += text;
		}
	}
}
