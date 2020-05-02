using System;
using System.Linq;
using System.Windows;

namespace MusicBox
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		public App()
		{
			this.DispatcherUnhandledException += App_DispatcherUnhandledException;
		}

		private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
		{
			var messageBox = new MessageBoxEx();

			messageBox.Title = "Unexpected Error";
			messageBox.Text =
				"An unexpected error has occurred. It may be possible to save your changes, but you should " +
				"close and restart MusicBox before continuing to use it. If you can repeatably get this " +
				"message to appear, please let Jonathan Gilbert know so we can stop it from happening.\n" +
				"\n" +
				"Error message: " + e.Exception.Message + "\n" +
				"\n" +
				"Stack trace:\n" +
				GetAbbreviatedStackTrace(e.Exception.StackTrace);

			messageBox.Width = Math.Min(
				1000,
				0.8 * SystemParameters.WorkArea.Width);

			messageBox.WindowStartupLocation = WindowStartupLocation.CenterScreen;

			messageBox.ShowDialog();

			e.Handled = true;
		}

		static string GetAbbreviatedStackTrace(string stackTrace)
		{
			string[] frames = stackTrace.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

			for (int i = frames.Length - 1; i >= 0; i--)
				if (frames[i].Contains("MainWindow"))
					return string.Join("\n", frames.Take(i + 1));

			return stackTrace;
		}
	}
}
