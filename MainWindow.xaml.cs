using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace MusicBox
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();

			_titleScrollAnimationInitiator = new Timer();
			_titleScrollAnimationInitiator.Elapsed += _titleScrollAnimationInitiator_Elapsed;

			AttachToTitleScrollOffsetPropertyChange();

			_player = new Player();

			_player.PlayingChanged +=
				(sender, e) =>
				{
					cmdPlay.Content = _player.Playing ? "Pause" : "Play";
				};

			_player.TotalTimeChanged +=
				(sender, e) =>
				{
					rLength.Text = FormatTimeSpan(_player.TotalTime);
					sTime.Maximum = _player.TotalTime.TotalSeconds;
				};

			_player.CurrentTimeChanged +=
				(sender, e) =>
				{
					_receivingTime = true;

					try
					{
						rCurrentTime.Text = FormatTimeSpan(_player.CurrentTime);
						sTime.Value = _player.CurrentTime.TotalSeconds;
					}
					finally
					{
						_receivingTime = false;
					}
				};

			_player.ReachedEnd +=
				(sender, e) =>
				{
					_player.CurrentTime = TimeSpan.Zero;
				};

			_player.TempoChanged +=
				(sender, e) =>
				{
					if (!_settingTempo)
						sTempo.Value = Math.Log(_player.Tempo, newBase: 2.0);
				};

			this.Playlist = new Playlist();

			_library = new Library(ConfigurationManager.AppSettings["LibraryPath"]);

			UpdateLibrarySearch();
		}

		static string FormatTimeSpan(TimeSpan span)
		{
			int minutes = (int)Math.Floor(span.TotalMinutes);
			int seconds = span.Seconds;

			return minutes + ":" + seconds.ToString("d2");
		}

		Player _player;
		Library _library;
		bool _receivingTime;
		bool _settingTempo;

		bool _draggingTitle;
		double _titleDragStartPosition;

		Timer _titleScrollAnimationInitiator;
		DoubleAnimation _titleScrollAnimation;

		public static readonly DependencyProperty PlaylistProperty = DependencyProperty.Register(nameof(Playlist), typeof(Playlist), typeof(MainWindow));

		public Playlist Playlist
		{
			get { return (Playlist)GetValue(PlaylistProperty); }
			set { SetValue(PlaylistProperty, value); }
		}

		private void lblTitle_MouseDown(object sender, MouseButtonEventArgs e)
		{
			lblTitle.CaptureMouse();
			_draggingTitle = true;

			var mousePosition = e.GetPosition(lblTitle);

			_titleDragStartPosition = mousePosition.X;

			_titleScrollAnimationInitiator.Interval = 25000;

			if (_titleScrollAnimation != null)
			{
				var scrollOffset = this.TitleScrollOffset;

				this.BeginAnimation(TitleScrollOffsetProperty, null);

				this.TitleScrollOffset = scrollOffset;

				_titleScrollAnimation = null;
			}
		}

		private void lblTitle_MouseMove(object sender, MouseEventArgs e)
		{
			if (_draggingTitle)
			{
				var mousePosition = e.GetPosition(lblTitle);

				double delta = mousePosition.X - _titleDragStartPosition;

				double newScrollPosition = svScrollTitle.HorizontalOffset - delta;

				svScrollTitle.ScrollToHorizontalOffset(newScrollPosition);
			}
		}

		private void lblTitle_MouseUp(object sender, MouseButtonEventArgs e)
		{
			if (_draggingTitle)
			{
				_draggingTitle = false;
				lblTitle.ReleaseMouseCapture();
			}
		}

		void SelectFile(string fileName)
		{
			_player.SelectFile(fileName);

			string title = Path.GetFileNameWithoutExtension(fileName);

			lblTitle.Content = title;
			lblTitle.FontStyle = FontStyles.Normal;

			svScrollTitle.ScrollToLeftEnd();

			_titleScrollAnimationInitiator.Interval = 5000;
			_titleScrollAnimationInitiator.Enabled = true;
		}

		void AttachToTitleScrollOffsetPropertyChange()
		{
			var propertyDescriptor = DependencyPropertyDescriptor.FromProperty(TitleScrollOffsetProperty, typeof(MainWindow));

			propertyDescriptor.AddValueChanged(
				this,
				(sender, e) =>
				{
					svScrollTitle.ScrollToHorizontalOffset(this.TitleScrollOffset);
				});
		}

		private void _titleScrollAnimationInitiator_Elapsed(object sender, ElapsedEventArgs e)
		{
			UI(() =>
			{
				double pixelsToScroll = lblTitle.ActualWidth - svScrollTitle.ActualWidth;

				if (pixelsToScroll < 0)
					_titleScrollAnimationInitiator.Enabled = false;
				else
				{
					const double LingerSeconds = 1.0;
					const double PixelsPerSecond = 80.0;

					var animationLength = TimeSpan.FromSeconds(LingerSeconds + pixelsToScroll / PixelsPerSecond);

					_titleScrollAnimation = new DoubleAnimation();

					_titleScrollAnimation.From = 0;
					_titleScrollAnimation.To = pixelsToScroll;
					_titleScrollAnimation.Duration = new Duration(animationLength);
					_titleScrollAnimation.BeginTime = TimeSpan.FromSeconds(LingerSeconds);

					// Cancel any previous animation that may be pinning the end value.
					this.BeginAnimation(TitleScrollOffsetProperty, null);

					this.TitleScrollOffset = 0.0;

					this.BeginAnimation(TitleScrollOffsetProperty, _titleScrollAnimation);

					_titleScrollAnimationInitiator.Interval = animationLength.TotalMilliseconds + 5000;
				}
			});
		}

		public static DependencyProperty TitleScrollOffsetProperty = DependencyProperty.Register(nameof(TitleScrollOffset), typeof(double), typeof(MainWindow));

		public double TitleScrollOffset
		{
			get { return (double)GetValue(TitleScrollOffsetProperty); }
			set { SetValue(TitleScrollOffsetProperty, value); }
		}

		private void cmdOpen_Click(object sender, RoutedEventArgs e)
		{
			var dialog = new OpenFileDialog();

			dialog.CheckFileExists = true;
			dialog.DereferenceLinks = true;
			dialog.Filter = "Audio Files (*.wav, *.mp3)|*.wav;*.mp3|All files (*.*)|*.*";
			dialog.Multiselect = false;
			dialog.Title = "Select Audio File";

			bool? result = dialog.ShowDialog(this);

			if (result ?? false)
				SelectFile(dialog.FileName);
		}

		private void cmdPlay_Click(object sender, RoutedEventArgs e)
		{
			if (_player.Playing)
				_player.Stop();
			else
				_player.Play();
		}

		private void sTime_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			if (!_receivingTime)
				_player.CurrentTime = TimeSpan.FromSeconds(e.NewValue);
		}

		private void cmdResetTempo_Click(object sender, RoutedEventArgs e)
		{
			_player.Tempo = 1.0;
		}

		private void sTempo_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			_settingTempo = true;

			try
			{
				_player.Tempo = Math.Pow(2.0, e.NewValue);
			}
			finally
			{
				_settingTempo = false;
			}
		}

		private void lstPlaylist_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (lstPlaylist.SelectedItem is FileReference fileReference)
				SelectFile(fileReference.FullPath);
		}

		double _libraryColumnWidthAtMouseDown;

		private void GridSplitter_MouseDown(object sender, MouseButtonEventArgs e)
		{
			_libraryColumnWidthAtMouseDown = cdLibraryColumn.ActualWidth;
		}

		private void GridSplitter_MouseUp(object sender, MouseButtonEventArgs e)
		{
			if (cdLibraryColumn.ActualWidth == _libraryColumnWidthAtMouseDown)
			{
				double minimumSize = grdRoot.ActualWidth * 0.08;

				double availableWidth = grdRoot.ActualWidth - cdSplitterColumn.ActualWidth;

				double playerExpandedSize = availableWidth * 0.6;
				double libraryExpandedSize = availableWidth * 0.4;

				double playerCollapsedSize = availableWidth;
				double libraryCollapsedSize = 0;

				void AnimateWidth(ColumnDefinition column, double targetWidth)
				{
					var fromWidth = column.Width;
					var toWidth = new GridLength(targetWidth, GridUnitType.Star);

					column.Width = toWidth;

					column.BeginAnimation(
						ColumnDefinition.WidthProperty,
						new GridLengthAnimation()
						{
							From = fromWidth,
							To = toWidth,
							Duration = TimeSpan.FromSeconds(0.2),
							AccelerationRatio = 0.4,
							DecelerationRatio = 0.4,
							FillBehavior = FillBehavior.Stop,
						});
				}

				if (cdLibraryColumn.ActualWidth < minimumSize)
				{
					AnimateWidth(cdPlayerColumn, playerExpandedSize);
					AnimateWidth(cdLibraryColumn, libraryExpandedSize);
				}
				else
				{
					AnimateWidth(cdPlayerColumn, playerCollapsedSize);
					AnimateWidth(cdLibraryColumn, libraryCollapsedSize);
				}
			}
		}

		private void txtLibrarySearch_TextChanged(object sender, TextChangedEventArgs e)
		{
			UpdateLibrarySearch();
		}

		void UpdateLibrarySearch()
		{
			string searchText = txtLibrarySearch.Text.Trim();

			bool isEmptySearch = string.IsNullOrWhiteSpace(searchText);

			var rootNodes = new List<SearchResultNode>();
			var currentNode = new List<(string PathComponent, SearchResultNode Node)>();

			foreach (var result in _library.Search(searchText))
			{
				if (result.Components.Count < currentNode.Count)
					currentNode.RemoveRange(result.Components.Count, currentNode.Count - result.Components.Count);

				for (int i = 0; i < result.Components.Count; i++)
				{
					if ((i >= currentNode.Count) || !result.Components[i].Name.Equals(currentNode[i].PathComponent))
					{
						var node = new SearchResultNode();

						if (i + 1 == result.Components.Count)
							node.SearchResult = result;

						node.NodeType = (i + 1 < result.Components.Count) ? SearchResultNodeType.Folder : SearchResultNodeType.File;
						node.Heading = CreateTextBlockForSearchResultComponent(result.Components[i]);
						node.IsExpanded = !isEmptySearch;

						if (i == 0)
							rootNodes.Add(node);
						else
						{
							var parentNode = currentNode[i - 1].Node;

							if (parentNode.ChildNodes == null)
								parentNode.ChildNodes = new System.Collections.ObjectModel.ObservableCollection<SearchResultNode>();

							parentNode.ChildNodes.Add(node);
						}

						if (currentNode.Count > i)
							currentNode.RemoveRange(i, currentNode.Count - i);

						currentNode.Add((result.Components[i].Name, node));
					}
				}
			}

			tvLibrarySearchResults.ItemsSource = rootNodes;
		}

		TextBlock CreateTextBlockForSearchResultComponent(SearchResultComponent component)
		{
			var textBlock = new TextBlock();

			foreach (var segment in component.Segments)
			{
				var run = new Run();

				run.FontWeight = (segment.Highlight ? FontWeights.Bold : FontWeights.Normal);
				run.Text = segment.Text;

				textBlock.Inlines.Add(run);
			}

			return textBlock;
		}

		string[] GetPathComponents(string path)
		{
			var components = new Stack<string>();

			while (!string.IsNullOrWhiteSpace(path))
			{
				components.Push(Path.GetFileName(path));

				path = Path.GetDirectoryName(path);
			}

			return components.ToArray();
		}

		void UI(Action action)
		{
			if (Dispatcher.CheckAccess())
				action();
			else
				Dispatcher.Invoke(action);
		}
	}
}
