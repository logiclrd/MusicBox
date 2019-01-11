using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace MusicBox
{
	/// <summary>
	/// Interaction logic for PlaylistDialog.xaml
	/// </summary>
	public partial class PlaylistDialog : Window
	{
		public PlaylistDialog()
		{
			InitializeComponent();

			this.Loaded += SavePlaylistDialog_Loaded;
		}

		private void SavePlaylistDialog_Loaded(object sender, RoutedEventArgs e)
		{
			if (lstPlaylists.SelectedIndex >= 0)
				lstPlaylists.Focus();
			else
			{
				txtPlaylistName.SelectAll();
				txtPlaylistName.Focus();
			}
		}

		ObservableCollection<FileReference> _playlistsModel;

		public void LoadPlaylists(IEnumerable<FileReference> playlists)
		{
			_playlistsModel = new ObservableCollection<FileReference>();

			foreach (var file in playlists.OrderBy(p => p.Title, StringComparer.OrdinalIgnoreCase))
				_playlistsModel.Add(file);

			lstPlaylists.ItemsSource = _playlistsModel;
			lstPlaylists.SelectedIndex = -1;
		}

		FileReference _playlistFile;

		public FileReference PlaylistFile
		{
			get { return _playlistFile; }
			set
			{
				if (value != null)
				{
					txtPlaylistName.Text = value.Title;

					foreach (var file in _playlistsModel.OfType<FileReference>())
						if (file.Title == value.Title)
						{
							_playlistFile = file;
							lstPlaylists.SelectedItem = file;
							txtPlaylistName.Text = file.Title;

							break;
						}
				}
			}
		}

		public event EventHandler DeleteSelectedPlaylist;

		private void cmdSave_Click(object sender, RoutedEventArgs e)
		{
			_playlistFile = null;

			foreach (var file in _playlistsModel)
				if (file.Title.Equals(txtPlaylistName.Text, StringComparison.OrdinalIgnoreCase))
				{
					_playlistFile = file;
					break;
				}

			if (_playlistFile == null)
			{
				_playlistFile =
					new FileReference()
					{
						Title = txtPlaylistName.Text
					};
			}

			DialogResult = true;
		}

		private void lstPlaylists_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
		{
			cmdDelete.IsEnabled = (lstPlaylists.SelectedIndex >= 0);
		}

		private void cmdDelete_Click(object sender, RoutedEventArgs e)
		{
			_playlistFile = lstPlaylists.SelectedItem as FileReference;

			if (_playlistFile != null)
			{
				var confirmDialog = new ConfirmDialog();

				confirmDialog.Prompt = "Permanently delete the dance list '" + _playlistFile.Title + "'? There is no undo available.";
				confirmDialog.Owner = this;

				bool? result;

				using (Curtain.ShowCurtain(grdRoot))
					result = confirmDialog.ShowDialog();

				if (result ?? false)
				{
					DeleteSelectedPlaylist?.Invoke(this, EventArgs.Empty);

					_playlistsModel.Remove(_playlistFile);
					_playlistFile = null;

					txtPlaylistName.Text = "";
					txtPlaylistName.Focus();
				}
			}
		}

		private void cmdCancel_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = false;
		}
	}
}
