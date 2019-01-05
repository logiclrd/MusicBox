using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

using GongSolutions.Wpf.DragDrop;

namespace MusicBox
{
	public class Playlist : ObservableCollection<FileReference>, IDropTarget
	{
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
