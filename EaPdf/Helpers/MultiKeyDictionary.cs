﻿// *************************************************
// Created by Aron Weiler
// Feel free to use this code in any way you like, 
// just don't blame me when your coworkers think you're awesome.
// Comments?  Email aronweiler@gmail.com
// Revision 1.6
// Revised locking strategy based on the some bugs found with the existing lock objects. 
// Possible deadlock and race conditions resolved by swapping out the two lock objects for a ReaderWriterLockSlim. 
// Performance takes a very small hit, but correctness is guaranteed.
// *************************************************
// 20223-03-14 Modified by TGH to use the new C# 9.0 syntax and to support setting the equality comparers on the internal dictionaries
// *************************************************

namespace Aron.Weiler
{
    /// <summary>
    /// Multi-Key Dictionary Class
    /// </summary>	
    /// <typeparam name="K">Primary Key Type</typeparam>
    /// <typeparam name="L">Sub Key Type</typeparam>
    /// <typeparam name="V">Value Type</typeparam>
    public class MultiKeyDictionary<K, L, V> where K : notnull where L : notnull
	{
		internal readonly Dictionary<K, V> baseDictionary;
		internal readonly Dictionary<L, K> subDictionary;
		internal readonly Dictionary<K, L> primaryToSubkeyMapping;
        private readonly ReaderWriterLockSlim readerWriterLock;

		public MultiKeyDictionary()
		{
			baseDictionary = new Dictionary<K, V>();
			subDictionary = new Dictionary<L, K>();
			primaryToSubkeyMapping = new Dictionary<K, L>();
			readerWriterLock = new ReaderWriterLockSlim();
		}

		public MultiKeyDictionary(IEqualityComparer<K> primaryKeyComparer, IEqualityComparer<L> subKeyComparer)
		{
            baseDictionary = new Dictionary<K, V>(primaryKeyComparer);
            subDictionary = new Dictionary<L, K>(subKeyComparer);
            primaryToSubkeyMapping = new Dictionary<K, L>(primaryKeyComparer);
            readerWriterLock = new ReaderWriterLockSlim();
        }

        public MultiKeyDictionary(IEqualityComparer<K> primaryKeyComparer)
        {
            baseDictionary = new Dictionary<K, V>(primaryKeyComparer);
            subDictionary = new Dictionary<L, K>();
            primaryToSubkeyMapping = new Dictionary<K, L>(primaryKeyComparer);
            readerWriterLock = new ReaderWriterLockSlim();
        }


        public V? this[L subKey]
		{
			get
			{
                if (TryGetValue(subKey, out V? item))
                    return item;

                throw new KeyNotFoundException("sub key not found: " + subKey.ToString());
			}
		}

		public V? this[K primaryKey]
		{
			get
			{
				if (TryGetValue(primaryKey, out V? item))
					return item;

				throw new KeyNotFoundException("primary key not found: " + primaryKey.ToString());
			}
		}

		public void Associate(L subKey, K primaryKey)
		{
			readerWriterLock.EnterUpgradeableReadLock();

			try
			{
				if (!baseDictionary.ContainsKey(primaryKey))
					throw new KeyNotFoundException(string.Format("The base dictionary does not contain the key '{0}'", primaryKey));

				if (primaryToSubkeyMapping.ContainsKey(primaryKey)) // Remove the old mapping first
				{
					readerWriterLock.EnterWriteLock();

					try
					{
						if (subDictionary.ContainsKey(primaryToSubkeyMapping[primaryKey]))
						{
							subDictionary.Remove(primaryToSubkeyMapping[primaryKey]);
						}

						primaryToSubkeyMapping.Remove(primaryKey);
					}
					finally
					{
						readerWriterLock.ExitWriteLock();
					}
				}

				subDictionary[subKey] = primaryKey;
				primaryToSubkeyMapping[primaryKey] = subKey;
			}
			finally
			{
				readerWriterLock.ExitUpgradeableReadLock();
			}
		}

		public bool TryGetValue(L subKey, out V? val)
		{
			val = default;


            readerWriterLock.EnterReadLock();

            try
			{
				if (subDictionary.TryGetValue(subKey, out K? primaryKey))
				{
					return baseDictionary.TryGetValue(primaryKey, out val);
				}
			}
			finally
			{
				readerWriterLock.ExitReadLock();
			}

			return false;
		}

		public bool TryGetValue(K primaryKey, out V? val)
		{
			readerWriterLock.EnterReadLock();

			try
			{
				return baseDictionary.TryGetValue(primaryKey, out val);
			}
			finally
			{
				readerWriterLock.ExitReadLock();
			}
		}

		public bool ContainsKey(L subKey)
		{
            return TryGetValue(subKey, out _);
		}

		public bool ContainsKey(K primaryKey)
		{
            return TryGetValue(primaryKey, out _);
		}

		public void Remove(K primaryKey)
		{
			readerWriterLock.EnterWriteLock();

			try
			{
				if (primaryToSubkeyMapping.ContainsKey(primaryKey))
				{
					if (subDictionary.ContainsKey(primaryToSubkeyMapping[primaryKey]))
					{
						subDictionary.Remove(primaryToSubkeyMapping[primaryKey]);
					}

					primaryToSubkeyMapping.Remove(primaryKey);
				}

				baseDictionary.Remove(primaryKey);
			}
			finally
			{
				readerWriterLock.ExitWriteLock();
			}
		}

		public void Remove(L subKey)
		{
			readerWriterLock.EnterWriteLock();

			try
			{
				baseDictionary.Remove(subDictionary[subKey]);

				primaryToSubkeyMapping.Remove(subDictionary[subKey]);

				subDictionary.Remove(subKey);
			}
			finally
			{
				readerWriterLock.ExitWriteLock();
			}
		}

		public void Add(K primaryKey, V val)
		{
			readerWriterLock.EnterWriteLock();

			try
			{
				baseDictionary.Add(primaryKey, val);
			}
			finally
			{
				readerWriterLock.ExitWriteLock();
			}
		}

		public void Add(K primaryKey, L subKey, V val)
		{
			Add(primaryKey, val);

			Associate(subKey, primaryKey);
		}

		public V[] CloneValues()
		{
			readerWriterLock.EnterReadLock();

			try
			{
				V[] values = new V[baseDictionary.Values.Count];

				baseDictionary.Values.CopyTo(values, 0);

				return values;
			}
			finally
			{
				readerWriterLock.ExitReadLock();
			}
		}

		public List<V> Values
		{
			get
			{
				readerWriterLock.EnterReadLock();

				try
				{
					return baseDictionary.Values.ToList();
				}
				finally
				{
					readerWriterLock.ExitReadLock();
				}
			}
		}

		public K[] ClonePrimaryKeys()
		{
			readerWriterLock.EnterReadLock();

			try
			{
				K[] values = new K[baseDictionary.Keys.Count];

				baseDictionary.Keys.CopyTo(values, 0);

				return values;
			}
			finally
			{
				readerWriterLock.ExitReadLock();
			}
		}

		public L[] CloneSubKeys()
		{
			readerWriterLock.EnterReadLock();

			try
			{
				L[] values = new L[subDictionary.Keys.Count];

				subDictionary.Keys.CopyTo(values, 0);

				return values;
			}
			finally
			{
				readerWriterLock.ExitReadLock();
			}
		}

		public void Clear()
		{
			readerWriterLock.EnterWriteLock();

			try
			{
				baseDictionary.Clear();

				subDictionary.Clear();

				primaryToSubkeyMapping.Clear();
			}
			finally
			{
				readerWriterLock.ExitWriteLock();
			}
		}

		public int Count
		{
			get
			{
				readerWriterLock.EnterReadLock();

				try
				{
					return baseDictionary.Count;
				}
				finally
				{
					readerWriterLock.ExitReadLock();
				}
			}
		}

		public IEnumerator<KeyValuePair<K, V>> GetEnumerator()
		{
			readerWriterLock.EnterReadLock();

			try
			{
				return baseDictionary.GetEnumerator();
			}
			finally
			{
				readerWriterLock.ExitReadLock();
			}
		}
	}
}
