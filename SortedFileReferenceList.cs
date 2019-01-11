using System;
using System.Collections;
using System.Collections.Generic;

namespace MusicBox
{
	public class SortedFileReferenceList : IList<FileReference>
	{
		List<FileReference> _list = new List<FileReference>();

		public int Count => _list.Count;
		public bool IsReadOnly => false;

		public FileReference this[int index]
		{
			get
			{
				return _list[index];
			}
			set
			{
				if (value.SortKey == _list[index].SortKey)
					_list[index] = value;
				else
				{
					_list.RemoveAt(index);
					Add(value);
				}
			}
		}

		public SortedFileReferenceList()
		{
		}

		public SortedFileReferenceList(IEnumerable<FileReference> items)
		{
			AddRange(items);
		}

		public void Add(FileReference item)
		{
			int index = _list.BinarySearch(item);

			if (index < 0)
				index = ~index;

			_list.Insert(index, item);

			item.SortKeyChanged += Item_SortKeyChanged;
		}

		public void AddRange(IEnumerable<FileReference> items)
		{
			foreach (var item in items)
				Add(item);
		}

		public void Insert(int index, FileReference item)
		{
			Add(item);
		}

		public void RemoveAt(int index)
		{
			if ((index >= 0) && (index < _list.Count))
				_list[index].SortKeyChanged -= Item_SortKeyChanged;

			_list.RemoveAt(index);
		}

		public void Clear()
		{
			_list.ForEach(item => item.SortKeyChanged -= Item_SortKeyChanged);
			_list.Clear();
		}

		bool _handlingSortKeyChanged = false;

		private void Item_SortKeyChanged(object sender, SortKeyChangedEventArgs e)
		{
			if (_handlingSortKeyChanged)
				return;

			_handlingSortKeyChanged = true;

			try
			{
				if (sender is FileReference item)
				{
					var surrogate = new FileReference() { SortKey = e.NewSortKey };

					item.SortKey = e.OldSortKey;

					int oldIndex = _list.BinarySearch(item);
					int newIndex = _list.BinarySearch(surrogate);

					if (oldIndex < 0)
						_list.Remove(item);
					else
						_list.RemoveAt(oldIndex);

					if (newIndex < 0)
						newIndex = ~newIndex;

					if (oldIndex < newIndex)
						newIndex--;

					item.SortKey = e.NewSortKey;

					_list.Insert(newIndex, item);
				}
			}
			finally
			{
				_handlingSortKeyChanged = false;
			}
		}

		public int IndexOf(FileReference item)
		{
			return _list.IndexOf(item);
		}

		public bool Contains(FileReference item)
		{
			return _list.Contains(item);
		}

		public void CopyTo(FileReference[] array)
		{
			_list.CopyTo(array);
		}

		public void CopyTo(FileReference[] array, int arrayIndex)
		{
			_list.CopyTo(array, arrayIndex);
		}

		public bool Remove(FileReference item)
		{
			bool isRemoved = _list.Remove(item);

			if (isRemoved)
				item.SortKeyChanged -= Item_SortKeyChanged;

			return isRemoved;
		}

		public IEnumerator<FileReference> GetEnumerator()
		{
			return _list.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return _list.GetEnumerator();
		}
	}
}
