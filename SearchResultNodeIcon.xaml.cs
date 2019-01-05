using System;
using System.Windows;
using System.Windows.Controls;

namespace MusicBox
{
	/// <summary>
	/// Interaction logic for SearchResultNodeIcon.xaml
	/// </summary>
	public partial class SearchResultNodeIcon : UserControl
	{
		public SearchResultNodeIcon()
		{
			InitializeComponent();
		}

		public static readonly DependencyProperty NodeTypeProperty = DependencyProperty.Register(nameof(NodeType), typeof(SearchResultNodeType), typeof(SearchResultNodeIcon), new PropertyMetadata(SearchResultNodeIcon_NodeTypeChanged));

		public SearchResultNodeType NodeType
		{
			get { return (SearchResultNodeType)GetValue(NodeTypeProperty); }
			set { SetValue(NodeTypeProperty, value); }
		}

		private static void SearchResultNodeIcon_NodeTypeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if ((d is SearchResultNodeIcon @this) && (e.NewValue is SearchResultNodeType nodeType))
			{
				@this.pFolder.Visibility = (nodeType == SearchResultNodeType.Folder) ? Visibility.Visible : Visibility.Collapsed;
				@this.pFile.Visibility = (nodeType == SearchResultNodeType.File) ? Visibility.Visible : Visibility.Collapsed;
			}
		}
	}
}
