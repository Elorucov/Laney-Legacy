using ELOR.VKAPILib.Objects;
using Elorucov.Laney.Core;
using Elorucov.Laney.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;

namespace Elorucov.Laney.Helpers
{
    public class MessagesCollection : ThreadSafeObservableCollection<MessageViewModel>
    {
        public GroupedMessagesCollection GroupedMessages { get; set; }

        public MessagesCollection() { }

        public MessagesCollection(List<MessageViewModel> messages, bool doNotGrouping = false)
        {
            foreach (MessageViewModel msg in messages)
            {
                Add(msg);
            }
            if (!doNotGrouping) CreateGroup();
        }

        public MessagesCollection(List<Message> messages, bool doNotGrouping = false)
        {
            foreach (Message msg in messages)
            {
                Add(new MessageViewModel(msg));
            }
            if (!doNotGrouping) CreateGroup();
        }

        public event EventHandler<MessageViewModel> Inserted;

        public void Insert(MessageViewModel message)
        {
            try
            {
                int idx = 0;
                var q = from m in Items where m.Id == message.Id select m;
                if (q.Count() == 1)
                {
                    MessageViewModel old = q.First();
                    idx = IndexOf(old);
                    RemoveMessage(old);
                    Insert(idx, message);
                }
                else
                {
                    idx = this.ToList().BinarySearch(message);
                    if (idx < 0) idx = ~idx;
                    Insert(idx, message);
                }
                if (GroupedMessages != null) GroupedMessages.Insert(message);
                Inserted?.Invoke(this, message);
            }
            catch (Exception ex)
            {
                if (ex.HResult == -2147467262)
                {
                    Log.General.Error("Classic error due to multithreads.");
                }
                else
                {
                    throw ex;
                }
            }
        }

        public void Insert(Message message)
        {
            Insert(new MessageViewModel(message));
        }

        public void RemoveMessage(MessageViewModel message)
        {
            // Cyka blet
            SynchronizationContext.Current.Post(o =>
            {
                Remove(message);
                if (GroupedMessages != null) GroupedMessages.Remove(message);
            }, null);
        }

        private void CreateGroup()
        {
            GroupedMessages = new GroupedMessagesCollection(this);
        }

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            base.OnCollectionChanged(e);
            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                if (GroupedMessages != null) GroupedMessages.Clear();
            }
        }
    }

    public class MessagesCollection<TKey> : MessagesCollection, IComparable
    {
        public MessagesCollection(TKey key, List<MessageViewModel> messages) : base(messages, true)
        {
            Key = key;
        }

        public TKey Key { get; private set; }

        public int CompareTo(object obj)
        {
            if (obj is MessagesCollection<DateTime> msgd && Key is DateTime dt)
            {
                return dt.CompareTo(msgd.Key);
            }
            throw new InvalidOperationException("No comparable TKey.");
        }
    }
}