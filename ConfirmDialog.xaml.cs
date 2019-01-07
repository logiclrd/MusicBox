using System;
using System.Windows;

namespace MusicBox
{
	/// <summary>
	/// Interaction logic for ConfirmDialog.xaml
	/// </summary>
	public partial class ConfirmDialog : Window
	{
		public ConfirmDialog()
		{
			InitializeComponent();
		}

		public string Prompt
		{
			get { return lblPrompt.Content as string; }
			set { lblPrompt.Content = value; }
		}

		private void cmdConfirm_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = true;
		}

		private void cmdCancel_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = false;
		}
	}
}
