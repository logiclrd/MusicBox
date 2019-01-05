using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace MusicBox
{
	public class FileReference : DependencyObject
	{
		public static readonly DependencyProperty FileNameProperty = DependencyProperty.Register(nameof(FileName), typeof(string), typeof(FileReference));
		public static readonly DependencyProperty RelativePathProperty = DependencyProperty.Register(nameof(RelativePath), typeof(string), typeof(FileReference));
		public static readonly DependencyProperty FullPathProperty = DependencyProperty.Register(nameof(FullPath), typeof(string), typeof(FileReference));

		public string FileName
		{
			get { return (string)GetValue(FileNameProperty); }
			set { SetValue(FileNameProperty, value); }
		}

		public string RelativePath
		{
			get { return (string)GetValue(RelativePathProperty); }
			set { SetValue(RelativePathProperty, value); }
		}

		public string FullPath
		{
			get { return (string)GetValue(FullPathProperty); }
			set { SetValue(FullPathProperty, value); }
		}

		public FileReference Clone()
		{
			return
				new FileReference()
				{
					FileName = this.FileName,
					RelativePath = this.RelativePath,
					FullPath = this.FullPath,
				};
		}
	}
}
