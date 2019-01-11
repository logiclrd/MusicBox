using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks.Dataflow;
using System.Windows.Threading;

namespace MusicBox
{
	class Library
	{
		string _path;
		FileSystemWatcher _watcher;
		SortedFileReferenceList _items;
		Dispatcher _dispatcher;

		public Library(string path)
		{
			_path = path;
			_dispatcher = Dispatcher.CurrentDispatcher;

			_resolveFileTitleQueue = new ActionBlock<QueueEntry>(new Action<QueueEntry>(ResolveFileTitle));

			_watcher = new FileSystemWatcher(path);
			_watcher.IncludeSubdirectories = true;
			_watcher.Created += _watcher_Created;
			_watcher.Renamed += _watcher_Renamed;
			_watcher.Deleted += _watcher_Deleted;
			_watcher.EnableRaisingEvents = true;

			_items = new SortedFileReferenceList(EnumerateFiles(path, ""));
		}

		class QueueEntry
		{
			public string FullPath;
			public FileReference FileReference;

			public QueueEntry(FileReference fileReference)
			{
				this.FullPath = fileReference.FullPath;
				this.FileReference = fileReference;
			}
		}

		ActionBlock<QueueEntry> _resolveFileTitleQueue;

		IEnumerable<FileReference> EnumerateFiles(string absolutePath, string relativePath)
		{
			foreach (var filePath in Directory.GetFiles(absolutePath))
			{
				string fileName = Path.GetFileName(filePath);

				if (Player.SupportedExtensions.Contains(Path.GetExtension(fileName)))
					yield return CreateFileReference(fileName, filePath, Path.Combine(relativePath, fileName));
			}

			foreach (var subdirectoryPath in Directory.GetDirectories(absolutePath))
			{
				string directoryName = Path.GetFileName(subdirectoryPath);

				foreach (var item in EnumerateFiles(subdirectoryPath, Path.Combine(relativePath, directoryName)))
					yield return item;
			}
		}

		FileReference CreateFileReference(string fileName, string filePath, string relativeFilePath)
		{
			var item = new FileReference();

			item.Title = Path.GetFileNameWithoutExtension(fileName);
			item.TitleIsFileName = true;
			item.FullPath = filePath;
			item.RelativePath = relativeFilePath;
			item.SortKey = Path.Combine(Path.GetDirectoryName(relativeFilePath), item.Title);

			_resolveFileTitleQueue.Post(new QueueEntry(item));

			return item;
		}

		void ResolveFileTitle(QueueEntry queueEntry)
		{
			try
			{
				using (var file = TagLib.File.Create(queueEntry.FullPath))
				{
					if ((file.Tag != null) && !string.IsNullOrWhiteSpace(file.Tag.Title))
					{
						_dispatcher.BeginInvoke((Action)(
							() =>
							{
								queueEntry.FileReference.Title = file.Tag.Title;
								queueEntry.FileReference.TitleIsFileName = false;
								queueEntry.FileReference.SortKey = Path.GetDirectoryName(queueEntry.FileReference.RelativePath) + "\\" + file.Tag.Title;
							}));
					}
				}
			}
			catch { }
		}

		string GetRelativePath(string fullPath)
		{
			var rootPathIdentifier = FileUtility.GetFileIdentifier(_path);

			// Immediately pull off the last component, since for deletions and the old full path of renames it won't actually exist any more.
			string relativePath = Path.GetFileName(fullPath);

			fullPath = Path.GetDirectoryName(fullPath);

			while ((fullPath.Length > 0) && (FileUtility.GetFileIdentifier(fullPath) != rootPathIdentifier))
			{
				relativePath = Path.Combine(Path.GetFileName(fullPath), relativePath);
				fullPath = Path.GetDirectoryName(fullPath);
			}

			return relativePath;
		}

		void AddPath(string fullPath)
		{
			string fileName = Path.GetFileName(fullPath);
			string relativePath = GetRelativePath(fullPath);

			_items.Add(CreateFileReference(fileName, fullPath, relativePath));
		}

		void RemovePath(string fullPath)
		{
			for (int i = 0; i < _items.Count; i++)
				if (_items[i].FullPath == fullPath)
				{
					_items.RemoveAt(i);
					break;
				}
		}

		private void _watcher_Created(object sender, FileSystemEventArgs e)
		{
			_dispatcher.Invoke(
				() =>
				{
					AddPath(e.FullPath);
				});
		}

		private void _watcher_Renamed(object sender, RenamedEventArgs e)
		{
			_dispatcher.Invoke(
				() =>
				{
					RemovePath(e.OldFullPath);
					AddPath(e.FullPath);
				});
		}

		private void _watcher_Deleted(object sender, FileSystemEventArgs e)
		{
			_dispatcher.Invoke(
				() =>
				{
					RemovePath(e.FullPath);
				});
		}

		public IEnumerable<SearchResult> Search(string substring)
		{
			if (!_dispatcher.CheckAccess())
			{
				IEnumerable<SearchResult> dispatchedResults = null;

				_dispatcher.Invoke(
					() =>
					{
						dispatchedResults = Search(substring);
					});

				return dispatchedResults;
			}

			var results = new List<SearchResult>();

			foreach (var item in _items)
			{
				string renderText = item.SortKey;

				int offset = (substring.Length == 0) ? -1 : renderText.IndexOf(substring, StringComparison.OrdinalIgnoreCase);

				if ((offset < 0) && (substring.Length > 0))
					continue;

				var result = new SearchResult(item, substring);

				result.RecomputeComponents();

				results.Add(result);
			}

			return results;
		}
	}
}
