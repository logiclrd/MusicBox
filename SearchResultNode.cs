using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace MusicBox
{
	public class SearchResultNode : DependencyObject
	{
		public static readonly DependencyProperty NodeTypeProperty = DependencyProperty.Register(nameof(NodeType), typeof(SearchResultNodeType), typeof(SearchResultNode));
		public static readonly DependencyProperty HeadingProperty = DependencyProperty.Register(nameof(Heading), typeof(TextBlock), typeof(SearchResultNode));
		public static readonly DependencyProperty ChildNodesProperty = DependencyProperty.Register(nameof(ChildNodes), typeof(ObservableCollection<SearchResultNode>), typeof(SearchResultNode));
		public static readonly DependencyProperty SearchResultProperty = DependencyProperty.Register(nameof(SearchResult), typeof(SearchResult), typeof(SearchResultNode));
		public static readonly DependencyProperty IsExpandedProperty = DependencyProperty.Register(nameof(IsExpanded), typeof(bool), typeof(SearchResultNode));

		public SearchResultNodeType NodeType
		{
			get { return (SearchResultNodeType)GetValue(NodeTypeProperty); }
			set { SetValue(NodeTypeProperty, value); }
		}

		public TextBlock Heading
		{
			get { return (TextBlock)GetValue(HeadingProperty); }
			set { SetValue(HeadingProperty, value); }
		}

		public ObservableCollection<SearchResultNode> ChildNodes
		{
			get { return (ObservableCollection<SearchResultNode>)GetValue(ChildNodesProperty); }
			set { SetValue(ChildNodesProperty, value); }
		}

		public SearchResult SearchResult
		{
			get { return (SearchResult)GetValue(SearchResultProperty); }
			set { SetValue(SearchResultProperty, value); }
		}

		public bool IsExpanded
		{
			get { return (bool)GetValue(IsExpandedProperty); }
			set { SetValue(IsExpandedProperty, value); }
		}
	}
}
