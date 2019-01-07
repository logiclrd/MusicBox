using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Xml.Serialization;

using GongSolutions.Wpf.DragDrop;

namespace MusicBox
{
	public class Playlist : ObservableCollection<FileReference>, IDropTarget
	{
		public string Name;

		[XmlRoot("Playlist")]
		public class POD
		{
			public string Name;
			[XmlArrayItem("File")]
			public FileReference[] Files;
		}

		static XmlSerializer s_serializer = new XmlSerializer(typeof(POD));

		public void Save(string fileName)
		{
			var pod = new POD();

			pod.Name = this.Name;
			pod.Files = this.ToArray();

			using (var stream = File.OpenWrite(fileName))
				s_serializer.Serialize(stream, pod);
		}

		public void Save(FileReference fileReference)
		{
			Save(fileReference.FullPath);
		}

		public static Playlist Load(string fileName)
		{
			using (var stream = File.OpenRead(fileName))
			{
				var pod = (POD)s_serializer.Deserialize(stream);

				var playlist = new Playlist();

				playlist.Name = pod.Name;

				foreach (var file in pod.Files)
					playlist.Add(file);

				return playlist;
			}
		}

		public static Playlist Load(FileReference fileReference)
		{
			return Load(fileReference.FullPath);
		}

		public static IEnumerable<FileReference> Enumerate(string basePath)
		{
			foreach (var filePath in Directory.GetFiles(basePath, "*.playlist"))
			{
				Playlist playlist;

				try
				{
					playlist = Playlist.Load(filePath);
				}
				catch
				{
					continue;
				}

				yield return
					new FileReference()
					{
						FileName = playlist.Name,
						FullPath = filePath,
					};
			}
		}

		public void DragOver(IDropInfo dropInfo)
		{
			if (dropInfo.Data is SearchResultNode)
			{
				dropInfo.DropTargetAdorner = DropTargetAdorners.Insert;
				dropInfo.Effects = DragDropEffects.Copy;
			}

			if (dropInfo.Data is FileReference fileReference)
			{
				dropInfo.DropTargetAdorner = DropTargetAdorners.Insert;
				dropInfo.Effects = this.Contains(fileReference) ? DragDropEffects.Move : DragDropEffects.Copy;
			}
		}

		public void Drop(IDropInfo dropInfo)
		{
			if (dropInfo.Data is SearchResultNode searchResultNode)
			{
				foreach (var searchResult in CollectSearchResults(searchResultNode))
				{
					var newFileReference = new FileReference();

					newFileReference.FileName = searchResult.Components.Last().Name;
					newFileReference.RelativePath = searchResult.RelativePath;
					newFileReference.FullPath = searchResult.FullPath;

					int insertionIndex;

					if (dropInfo.TargetItem is FileReference insertionPoint)
					{
						insertionIndex = this.IndexOf(insertionPoint);

						if (insertionIndex < 0)
							insertionIndex = 0;
					}
					else
						insertionIndex = this.Count;

					this.Insert(insertionIndex, newFileReference);
				}
			}

			if (dropInfo.Data is FileReference fileReference)
			{
				if (this.Contains(fileReference))
					this.Remove(fileReference);
				else
					fileReference = fileReference.Clone();

				int insertionIndex;

				if (dropInfo.TargetItem is FileReference insertionPoint)
				{
					insertionIndex = this.IndexOf(insertionPoint);

					if (insertionIndex < 0)
						insertionIndex = 0;
				}
				else
					insertionIndex = this.Count;

				this.Insert(insertionIndex, fileReference);
			}
		}

		IEnumerable<SearchResult> CollectSearchResults(SearchResultNode searchResultNode)
		{
			if (searchResultNode.SearchResult != null)
				yield return searchResultNode.SearchResult;

			if (searchResultNode.ChildNodes != null)
			{
				foreach (var childNode in searchResultNode.ChildNodes)
					foreach (var searchResult in CollectSearchResults(childNode))
						yield return searchResult;
			}
		}
	}
}
