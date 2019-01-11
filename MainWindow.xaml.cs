using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Animation;

using GongSolutions.Wpf.DragDrop;

namespace MusicBox
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			_libraryPath = ConfigurationManager.AppSettings["LibraryPath"];
			_playlistPath = ConfigurationManager.AppSettings["PlaylistPath"];

			EnsureDirectory(_libraryPath, "find music");
			EnsureDirectory(_playlistPath, "store dance lists");

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

			_library = new Library(_libraryPath);

			UpdateLibrarySearch();

			tvLibrarySearchResults.SetValue(GongSolutions.Wpf.DragDrop.DragDrop.DragHandlerProperty, new SetAllowDrop(cmdClearPlaylist, false));
			lstPlaylist.SetValue(GongSolutions.Wpf.DragDrop.DragDrop.DragHandlerProperty, new SetAllowDrop(cmdClearPlaylist, true));
		}

		class SetAllowDrop : DefaultDragHandler
		{
			UIElement _targetElement;
			bool _allowDrop;

			public SetAllowDrop(UIElement targetElement, bool allowDrop)
			{
				_targetElement = targetElement;
				_allowDrop = allowDrop;
			}

			public override void StartDrag(IDragInfo dragInfo)
			{
				_targetElement.AllowDrop = _allowDrop;
				base.StartDrag(dragInfo);
			}
		}

		void EnsureDirectory(string path, string usage)
		{
			if (!Directory.Exists(path))
			{
				try
				{
					Directory.CreateDirectory(path);
				}
				catch
				{
					MessageBox.Show($"Configuration problem: Expecting to {usage} in {path}, but that directory does not exist!", "Problem");
					Environment.Exit(1);
				}
			}
		}

		static string FormatTimeSpan(TimeSpan span)
		{
			int minutes = (int)Math.Floor(span.TotalMinutes);
			int seconds = span.Seconds;

			return minutes + ":" + seconds.ToString("d2");
		}

		string _libraryPath;
		string _playlistPath;

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

		FileReference _playlistFile;

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

		private void cmdExit_Click(object sender, RoutedEventArgs e)
		{
			Application.Current.Shutdown();
		}

		void SelectFile(FileReference file)
		{
			_player.SelectFile(file.FullPath);

			lblTitle.SetBinding(
				Label.ContentProperty,
				new Binding()
				{
					Source = file,
					Path = new PropertyPath(FileReference.TitleProperty),
				});

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
			{
				SelectFile(fileReference);

				if (_playlistTouchDragTimer == null)
				{
					_playlistTouchDragTimer = new Timer();
					_playlistTouchDragTimer.Interval = 800;
					_playlistTouchDragTimer.Elapsed += _playlistTouchDragTimer_Elapsed;
				}

				_playlistTouchDragTimer.Stop();
				lstPlaylist.SetValue(ScrollViewer.PanningModeProperty, PanningMode.None);
				_playlistTouchDragTimer.Start();
			}
		}

		private void _playlistTouchDragTimer_Elapsed(object sender, ElapsedEventArgs e)
		{
			_playlistTouchDragTimer.Stop();

			UI(() =>
			{
				lstPlaylist.SetValue(ScrollViewer.PanningModeProperty, PanningMode.Both);
			});
		}

		Timer _playlistTouchDragTimer;

		private void lstPlaylist_ManipulationBoundaryFeedback(object sender, ManipulationBoundaryFeedbackEventArgs e)
		{
			// By default, when you touch pan to the end of the scrollable area, the whole window moves around. I think this looks ugly for this app!
			e.Handled = true;
		}

		private void cmdClearPlaylist_Click(object sender, RoutedEventArgs e)
		{
			this.Playlist.Clear();

			_playlistFile = null;
		}

		private void cmdClearPlaylist_DragOver(object sender, DragEventArgs e)
		{
			e.Effects = DragDropEffects.None;

			string dataFormat = GongSolutions.Wpf.DragDrop.DragDrop.DataFormat.Name;

			if (!e.Data.GetDataPresent(dataFormat))
				return;

			if ((e.Data.GetData(dataFormat) is FileReference fileReference)
			 && Playlist.Contains(fileReference))
				e.Effects = DragDropEffects.Move;
		}

		private void cmdClearPlaylist_Drop(object sender, DragEventArgs e)
		{
			string dataFormat = GongSolutions.Wpf.DragDrop.DragDrop.DataFormat.Name;

			if (!e.Data.GetDataPresent(dataFormat))
				return;

			if (e.Data.GetData(dataFormat) is FileReference fileReference)
			{
				if (ReferenceEquals(lstPlaylist.SelectedItem, fileReference)
				 && (lstPlaylist.SelectedIndex >= 0)
				 && (lstPlaylist.SelectedIndex < Playlist.Count)
				 && ReferenceEquals(Playlist[lstPlaylist.SelectedIndex], fileReference))
					Playlist.RemoveAt(lstPlaylist.SelectedIndex);
				else
					Playlist.Remove(fileReference);
			}
		}

		private void cmdSavePlaylist_Click(object sender, RoutedEventArgs e)
		{
			var playlistDialog = new PlaylistDialog();

			playlistDialog.LoadPlaylists(Playlist.Enumerate(_playlistPath));
			playlistDialog.PlaylistFile = _playlistFile;

			playlistDialog.Owner = this;

			playlistDialog.DeleteSelectedPlaylist +=
				(innerSender, innerE) =>
				{
					if ((playlistDialog.PlaylistFile != null)
					 && File.Exists(playlistDialog.PlaylistFile.FullPath))
						File.Delete(playlistDialog.PlaylistFile.FullPath);
				};

			bool? result;

			using (Curtain.ShowCurtain(grdRoot))
				result = playlistDialog.ShowDialog();

			if (result ?? false)
			{
				_playlistFile = playlistDialog.PlaylistFile;

				if (_playlistFile.FullPath == null)
				{
					var sanitizedName = SanitizeFileName(_playlistFile.Title);

					if (sanitizedName.Length == 0)
						sanitizedName = "Playlist";

					_playlistFile.FullPath = Path.Combine(_playlistPath, sanitizedName + ".playlist");

					int index = 1;

					while (File.Exists(_playlistFile.FullPath))
					{
						index++;
						_playlistFile.FullPath = Path.Combine(_playlistPath, sanitizedName + "-" + index + ".playlist");
					}
				}

				this.Playlist.Name = _playlistFile.Title;
				this.Playlist.Save(_playlistFile);
			}
		}

		private string SanitizeFileName(string itemName)
		{
			var fileNameBuffer = new StringBuilder();

			itemName = itemName.TrimStart().TrimEnd(new[] { ' ', '.' });

			for (int i=0; i < itemName.Length; i++)
			{
				var ch = itemName[i];

				switch (ch)
				{
					case '<': ch = '('; break;
					case '>': ch = ')'; break;
					case ':': ch = ','; break;
					case '"': ch = '\''; break;
					case '/': ch = ','; break;
					case '\\': ch = ','; break;
					case '|': ch = ','; break;
					case '?': ch = '_'; break;
					case '*': ch = '_'; break;
				}

				if (ch < 32)
					ch = '_';

				fileNameBuffer.Append(ch);
			}

			string fileName = fileNameBuffer.ToString();

			if (ReservedFileNames.Contains(fileName))
				fileName += "_1";

			if (fileName.Length > 240)
				fileName = fileName.Substring(0, 240);

			return fileName;
		}

		static readonly HashSet<string> ReservedFileNames = new HashSet<string>(
			new[]
			{
				"CON", "PRN", "AUX", "NUL",
				"COM0", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
				"LPT0", "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9",
			},
			StringComparer.OrdinalIgnoreCase);

		private void cboSavedPlaylists_DropDownOpened(object sender, EventArgs e)
		{
			string playlistDirectory = ConfigurationManager.AppSettings["PlaylistPath"];

			List<FileReference> playlists = Playlist.Enumerate(playlistDirectory).ToList();

			playlists.Sort((l, r) => string.Compare(l.Title, r.Title, StringComparison.OrdinalIgnoreCase));

			if (playlists.Count == 0)
				playlists.Add(new FileReference() { Title = "No saved dance lists." });

			cboSavedPlaylists.SelectedIndex = -1;
			cboSavedPlaylists.ItemsSource = playlists;
		}

		private void cboSavedPlaylists_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			_playlistFile = cboSavedPlaylists.SelectedItem as FileReference;

			if (_playlistFile != null)
				this.Playlist = Playlist.Load(_playlistFile);
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

		string _lastSearchText;

		void UpdateLibrarySearch()
		{
			string searchText = txtLibrarySearch.Text.Trim();

			if (searchText.Length < 3)
				searchText = "";

			if (searchText == _lastSearchText)
				return;

			_lastSearchText = searchText;

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
						{
							node.SearchResult = result;

							result.ComponentsChanged +=
								(sender, e) =>
								{
									node.Heading = CreateTextBlockForSearchResultComponent(result.Components.Last());
								};
						}

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

		private void tvLibrarySearchResults_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
		{
			if (_libraryTouchDragTimer == null)
			{
				_libraryTouchDragTimer = new Timer();
				_libraryTouchDragTimer.Interval = 800;
				_libraryTouchDragTimer.Elapsed += _libraryTouchDragTimer_Elapsed;
			}

			_libraryTouchDragTimer.Stop();
			tvLibrarySearchResults.SetValue(ScrollViewer.PanningModeProperty, PanningMode.None);
			_libraryTouchDragTimer.Start();
		}

		private void _libraryTouchDragTimer_Elapsed(object sender, ElapsedEventArgs e)
		{
			_libraryTouchDragTimer.Stop();

			UI(() =>
			{
				tvLibrarySearchResults.SetValue(ScrollViewer.PanningModeProperty, PanningMode.Both);
			});
		}

		Timer _libraryTouchDragTimer;

		private void tvLibrarySearchResults_ManipulationBoundaryFeedback(object sender, ManipulationBoundaryFeedbackEventArgs e)
		{
			// By default, when you touch pan to the end of the scrollable area, the whole window moves around. I think this looks ugly for this app!
			e.Handled = true;
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
