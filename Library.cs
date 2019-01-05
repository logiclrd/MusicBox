using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicBox
{
	class Library
	{
		string _path;
		FileSystemWatcher _watcher;
		object _sync;
		List<string> _items;

		public Library(string path)
		{
			_path = path;

			_sync = new object();

			lock (_sync)
			{
				_watcher = new FileSystemWatcher(path);
				_watcher.IncludeSubdirectories = true;
				_watcher.Created += _watcher_Created;
				_watcher.Renamed += _watcher_Renamed;
				_watcher.Deleted += _watcher_Deleted;
				_watcher.EnableRaisingEvents = true;

				_items = new List<string>(EnumerateFiles(path, ""));
				_items.Sort(StringComparer.InvariantCultureIgnoreCase);
			}
		}

		IEnumerable<string> EnumerateFiles(string absolutePath, string relativePath)
		{
			foreach (var filePath in Directory.GetFiles(absolutePath))
			{
				string fileName = Path.GetFileName(filePath);

				yield return Path.Combine(relativePath, fileName);
			}

			foreach (var subdirectoryPath in Directory.GetDirectories(absolutePath))
			{
				string directoryName = Path.GetFileName(subdirectoryPath);

				foreach (var path in EnumerateFiles(subdirectoryPath, Path.Combine(relativePath, directoryName)))
					yield return path;
			}
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
			string relativePath = GetRelativePath(fullPath);

			int index = _items.BinarySearch(relativePath, StringComparer.InvariantCultureIgnoreCase);

			if (index < 0)
				_items.Insert(~index, relativePath);
		}

		void RemovePath(string fullPath)
		{
			string relativePath = GetRelativePath(fullPath);

			int index = _items.BinarySearch(relativePath, StringComparer.InvariantCultureIgnoreCase);

			if (index >= 0)
				_items.RemoveAt(index);
		}

		private void _watcher_Created(object sender, FileSystemEventArgs e)
		{
			lock (_sync)
				AddPath(e.FullPath);
		}

		private void _watcher_Renamed(object sender, RenamedEventArgs e)
		{
			lock (_sync)
			{
				RemovePath(e.OldFullPath);
				AddPath(e.FullPath);
			}
		}

		private void _watcher_Deleted(object sender, FileSystemEventArgs e)
		{
			lock (_sync)
				RemovePath(e.FullPath);
		}

		public IEnumerable<SearchResult> Search(string substring)
		{
			var results = new List<SearchResult>();

			lock (_sync)
			{
				foreach (var item in _items)
				{
					string renderText = item;

					int offset = (substring.Length == 0) ? -1 : renderText.IndexOf(substring, StringComparison.InvariantCultureIgnoreCase);

					if ((offset < 0) && (substring.Length > 0))
						continue;

					var result = new SearchResult();

					while (offset >= 0)
					{
						result.AddSegment(renderText.Substring(0, offset), highlight: false);
						result.AddSegment(renderText.Substring(offset, substring.Length), highlight: true);

						renderText = renderText.Substring(offset + substring.Length);
						offset = renderText.IndexOf(substring, StringComparison.InvariantCultureIgnoreCase);
					}

					if (renderText.Length > 0)
						result.AddSegment(renderText, highlight: false);

					result.RelativePath = item;
					result.FullPath = Path.Combine(_path, item);

					results.Add(result);
				}
			}

			return results;
		}
	}
}
