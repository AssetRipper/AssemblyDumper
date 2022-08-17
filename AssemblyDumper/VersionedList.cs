using System.Collections;

namespace AssetRipper.AssemblyDumper
{
	internal sealed class VersionedList<T> : IList<KeyValuePair<UnityVersion, T>>
	{
		private readonly List<KeyValuePair<UnityVersion, T>> _list = new();

		public KeyValuePair<UnityVersion, T> this[int index] { get => _list[index]; set => _list[index] = value; }

		public int Count => _list.Count;

		public int Capacity { get => _list.Capacity; set => _list.Capacity = value; }

		public bool IsReadOnly => false;

		private UnityVersion MostRecentVersion
		{
			get => Count == 0 ? default : this[Count - 1].Key;
		}

		public void Add(KeyValuePair<UnityVersion, T> pair)
		{
			if(pair.Key <= MostRecentVersion)
			{
				throw new Exception($"Version {pair.Key} was not greater than the most recent version {MostRecentVersion}");
			}
			else
			{
				_list.Add(pair);
			}
		}

		public void Add(UnityVersion version, T item) => Add(new KeyValuePair<UnityVersion, T>(version, item));

		public void Clear()
		{
			_list.Clear();
		}

		public bool Contains(KeyValuePair<UnityVersion, T> item)
		{
			return _list.Contains(item);
		}

		public void CopyTo(KeyValuePair<UnityVersion, T>[] array, int arrayIndex)
		{
			_list.CopyTo(array, arrayIndex);
		}

		public IEnumerator<KeyValuePair<UnityVersion, T>> GetEnumerator()
		{
			return _list.GetEnumerator();
		}

		public T? GetItemForVersion(UnityVersion version)
		{
			if (Count == 0 || this[0].Key > version)
			{
				return default;
			}

			for (int i = 0; i < Count - 1; i++)
			{
				if (this[i].Key <= version && version < this[i + 1].Key)
				{
					return this[i].Value;
				}
			}

			return this[Count - 1].Value;
		}

		public UnityVersionRange GetRange(int index)
		{
			return index == Count - 1
				? new UnityVersionRange(this[index].Key, UnityVersion.MaxVersion)
				: new UnityVersionRange(this[index].Key, this[index + 1].Key);
		}

		public UnityVersionRange GetRangeForItem(T item)
		{
			for (int i = 0; i < Count; i++)
			{
				if (EqualityComparer<T>.Default.Equals(this[i].Value, item))
				{
					return GetRange(i);
				}
			}

			throw new Exception($"Item not found: {item}");
		}

		public int IndexOf(KeyValuePair<UnityVersion, T> item)
		{
			return _list.IndexOf(item);
		}

		public void Insert(int index, KeyValuePair<UnityVersion, T> item)
		{
			throw new NotSupportedException();
		}

		public bool Remove(KeyValuePair<UnityVersion, T> item)
		{
			throw new NotSupportedException();
		}

		public void RemoveAt(int index)
		{
			throw new NotSupportedException();
		}

		public void Pop()
		{
			if(Count > 0)
			{
				_list.RemoveAt(Count - 1);
			}
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable)_list).GetEnumerator();
		}

		public IEnumerable<UnityVersion> Keys => _list.Select(x => x.Key);

		public IEnumerable<T> Values => _list.Select(x => x.Value);
	}
}
