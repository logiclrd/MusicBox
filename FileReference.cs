using System;
using System.Windows;

namespace MusicBox
{
	public class FileReference : DependencyObject, IComparable<FileReference>
	{
		public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(nameof(Title), typeof(string), typeof(FileReference));
		public static readonly DependencyProperty TitleIsFileNameProperty = DependencyProperty.Register(nameof(TitleIsFileName), typeof(bool), typeof(FileReference));
		public static readonly DependencyProperty RelativePathProperty = DependencyProperty.Register(nameof(RelativePath), typeof(string), typeof(FileReference));
		public static readonly DependencyProperty FullPathProperty = DependencyProperty.Register(nameof(FullPath), typeof(string), typeof(FileReference));
		public static readonly DependencyProperty SortKeyProperty = DependencyProperty.Register(nameof(SortKey), typeof(string), typeof(FileReference), new PropertyMetadata(FileReference_SortKeyChanged));

		private static void FileReference_SortKeyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (d is FileReference fileReference)
				fileReference.SortKeyChanged?.Invoke(fileReference, new SortKeyChangedEventArgs((string)e.OldValue, (string)e.NewValue));
		}

		public event SortKeyChangedEventHandler SortKeyChanged;

		public string Title
		{
			get { return (string)GetValue(TitleProperty); }
			set { SetValue(TitleProperty, value); }
		}

		public bool TitleIsFileName
		{
			get { return (bool)GetValue(TitleIsFileNameProperty); }
			set { SetValue(TitleIsFileNameProperty, value); }
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

		public string SortKey
		{
			get { return (string)GetValue(SortKeyProperty); }
			set { SetValue(SortKeyProperty, value); }
		}

		public int CompareTo(FileReference other)
		{
			return string.Compare(this.SortKey, other.SortKey, StringComparison.OrdinalIgnoreCase);
		}
	}
}
