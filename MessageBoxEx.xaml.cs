using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace MusicBox
{
	/// <summary>
	/// Interaction logic for MessageBoxEx.xaml
	/// </summary>
	public partial class MessageBoxEx : Window
	{
		public MessageBoxEx()
		{
			this.SourceInitialized += MessageBoxEx_SourceInitialized;

			InitializeComponent();
		}

		private void MessageBoxEx_SourceInitialized(object sender, EventArgs e)
		{
			var handleSource = new WindowInteropHelper(this);

			SetWindowLong(
				handleSource.Handle,
				GWL_EXSTYLE,
				GetWindowLong(handleSource.Handle, GWL_EXSTYLE) | WS_EX_DLGMODALFRAME);

			SetWindowPos(
				handleSource.Handle, IntPtr.Zero,
				0, 0, 0, 0,
				SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_FRAMECHANGED);
		}

		[DllImport("user32")]
		static extern int GetWindowLong(IntPtr hWnd, int nIndex);
		[DllImport("user32")]
		static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
		[DllImport("user32")]
		static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

		const int GWL_EXSTYLE = -20;

		const int WS_EX_DLGMODALFRAME = 0x0001;

		const int SWP_NOSIZE = 0x0001;
		const int SWP_NOMOVE = 0x0002;
		const int SWP_NOZORDER = 0x0004;
		const int SWP_FRAMECHANGED = 0x0020;

		public string Text
		{
			get => tbMessage.Text;
			set => tbMessage.Text = value;
		}

		private void cmdOK_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = true;
		}

		private void Window_KeyDown(object sender, KeyEventArgs e)
		{
			if ((e.Key == Key.C) && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
			{
				e.Handled = true;

				string text =
					"[Window Title]\n" +
					this.Title + "\n" +
					"\n" +
					"[Content]\n" +
					this.Text + "\n" +
					"\n" +
					"[OK]";

				for (int i = 0; i < 10; i++)
				{
					try
					{
						Clipboard.SetText(text);
						return;
					}
					catch
					{
						Thread.Sleep(10 * i);
					}
				}
			}
		}
	}
}
