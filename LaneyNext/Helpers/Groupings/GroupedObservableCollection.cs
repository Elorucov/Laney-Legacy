using System;
using System.Collections.Generic;
using System.Linq;

namespace Elorucov.Laney.Helpers.Groupings
{
    public class GroupedObservableCollection<TKey, TElement> : ThreadSafeObservableCollection<Grouping<TKey, TElement>> where TKey : IComparable<TKey>
    {
        private readonly Func<TElement, TKey> readKey;

        private Grouping<TKey, TElement> lastEffectedGroup;

        public IEnumerable<TKey> Keys => from i in this select i.Key;

        public GroupedObservableCollection(Func<TElement, TKey> readKey)
        {
            this.readKey = readKey;
        }

        public GroupedObservableCollection(Func<TElement, TKey> readKey, IEnumerable<TElement> items)
            : this(readKey)
        {
            foreach (TElement item in items)
            {
                Add(item);
            }
        }

        public bool Contains(TElement item)
        {
            return Contains(item, (TElement a, TElement b) => a.Equals(b));
        }

        public bool Contains(TElement item, Func<TElement, TElement, bool> compare)
        {
            TKey key = readKey(item);
            return TryFindGroup(key)?.Any((TElement i) => compare(item, i)) ?? false;
        }

        public IEnumerable<TElement> EnumerateItems()
        {
            return this.SelectMany((Grouping<TKey, TElement> g) => g);
        }

        public void Add(TElement item)
        {
            TKey key = readKey(item);
            FindOrCreateGroup(key).Add(item);
        }

        public void AddFirst(TElement item)
        {
            TKey key = readKey(item);
            FindOrCreateGroup(key).Insert(0, item);
        }

        public GroupedObservableCollection<TKey, TElement> ReplaceWith(GroupedObservableCollection<TKey, TElement> replacementCollection, IEqualityComparer<TElement> itemComparer)
        {
            List<TKey> list = replacementCollection.Keys.ToList();
            HashSet<TKey> first = new HashSet<TKey>(Keys);
            RemoveGroups(first.Except(list));
            EnsureGroupsExist(list);
            for (int i = 0; i < replacementCollection.Count; i++)
            {
                MergeGroup(base[i], replacementCollection[i], itemComparer);
            }
            return this;
        }

        private static void MergeGroup(Grouping<TKey, TElement> current, Grouping<TKey, TElement> replacement, IEqualityComparer<TElement> itemComparer)
        {
            if (current.SequenceEqual(replacement, itemComparer))
            {
                return;
            }
            HashSet<TElement> second = new HashSet<TElement>(replacement, itemComparer);
            foreach (TElement item in current.Except(second, itemComparer).ToList())
            {
                current.Remove(item);
            }
            HashSet<TElement> hashSet = new HashSet<TElement>(current, itemComparer);
            for (int i = 0; i < replacement.Count; i++)
            {
                TElement val = replacement[i];
                if (i < current.Count && itemComparer.Equals(current[i], val))
                {
                    continue;
                }
                if (hashSet.Contains(val))
                {
                    for (int j = i + 1; j < current.Count; j++)
                    {
                        if (itemComparer.Equals(current[j], val))
                        {
                            current.Move(i, j);
                            break;
                        }
                    }
                }
                else
                {
                    current.Insert(i, replacement[i]);
                }
            }
        }

        private void EnsureGroupsExist(IList<TKey> requiredKeys)
        {
            if (base.Count == requiredKeys.Count)
            {
                return;
            }
            for (int i = 0; i < requiredKeys.Count; i++)
            {
                if (base.Count <= i || !base[i].Key.Equals(requiredKeys[i]))
                {
                    Insert(i, new Grouping<TKey, TElement>(requiredKeys[i]));
                }
            }
        }

        private void RemoveGroups(IEnumerable<TKey> keys)
        {
            HashSet<TKey> hashSet = new HashSet<TKey>(keys);
            for (int num = base.Count - 1; num >= 0; num--)
            {
                if (hashSet.Contains(base[num].Key))
                {
                    RemoveAt(num);
                }
            }
        }

        public bool Remove(TElement item)
        {
            TKey key = readKey(item);
            Grouping<TKey, TElement> grouping = TryFindGroup(key);
            bool result = grouping?.Remove(item) ?? false;
            if (grouping != null && grouping.Count == 0)
            {
                Remove(grouping);
                lastEffectedGroup = null;
            }
            return result;
        }

        private Grouping<TKey, TElement> TryFindGroup(TKey key)
        {
            if (lastEffectedGroup != null && lastEffectedGroup.Key.Equals(key))
            {
                return lastEffectedGroup;
            }
            return lastEffectedGroup = this.FirstOrDefault((Grouping<TKey, TElement> i) => i.Key.Equals(key));
        }

        private Grouping<TKey, TElement> FindOrCreateGroup(TKey key)
        {
            if (lastEffectedGroup != null && lastEffectedGroup.Key.Equals(key))
            {
                return lastEffectedGroup;
            }
            var anon = this.Select((Grouping<TKey, TElement> group, int index) => new
            {
                group,
                index
            }).FirstOrDefault(i => i.group.Key.CompareTo(key) >= 0);
            Grouping<TKey, TElement> grouping;
            if (anon == null)
            {
                grouping = new Grouping<TKey, TElement>(key);
                Add(grouping);
            }
            else if (!anon.group.Key.Equals(key))
            {
                grouping = new Grouping<TKey, TElement>(key);
                Insert(anon.index, grouping);
            }
            else
            {
                grouping = anon.group;
            }
            lastEffectedGroup = grouping;
            return grouping;
        }
    }
}
