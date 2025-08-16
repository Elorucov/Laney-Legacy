using Elorucov.Laney.Models;
using Elorucov.Laney.Services.ListHelpers;
using Elorucov.VkAPI.Objects;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;

namespace Elorucov.Laney.Services.UI {
    public class GroupedMessagesCollection : ThreadSafeObservableCollection<MessagesCollectionGroupItem> {
        public GroupedMessagesCollection(MessagesCollection messages) {
            if (messages != null && messages.Count > 0) {
                var groups = messages.GroupBy(m => m.Date.Date);
                foreach (var g in groups) {
                    MessagesCollectionGroupItem mc = new MessagesCollectionGroupItem(g.Key, g.ToList());
                    Add(mc);
                    Debug.WriteLine($"GroupedMessagesCollection: Key = {g.Key}, Count = {mc.Count}");
                }
            }
        }

        public void Insert(LMessage message) {
            var q = from g in Items where g.Key == message.Date.Date select g;
            if (q.Count() == 1) {
                q.First().Insert(message);
                Debug.WriteLine($"GroupedMessagesCollection: Message {message.ConversationMessageId} inserted in group {q.First().Key}");
            } else {
                MessagesCollectionGroupItem group = new MessagesCollectionGroupItem(message.Date.Date, new List<LMessage> { message });
                int idx = this.ToList().BinarySearch(group);
                if (idx < 0) idx = ~idx;
                Insert(idx, group);
                Debug.WriteLine($"GroupedMessagesCollection: Message {message.ConversationMessageId} inserted in new group (idx: {idx})");
            }
        }

        public void Remove(LMessage message) {
            var q = from g in Items where g.Key == message.Date.Date select g;
            if (q.Count() == 1) {
                MessagesCollectionGroupItem i = q.FirstOrDefault();
                if (i != null) {
                    i.Remove(message);
                    if (i.Count == 0) Remove(i);
                    Debug.WriteLine($"GroupedMessagesCollection: Message {message.ConversationMessageId} removed from group {i.Key}");
                }
            }
        }
    }

    public class MessagesCollection : ObservableCollection<LMessage> {
        public GroupedMessagesCollection GroupedMessages { get; set; }

        public MessagesCollection() { }

        public MessagesCollection(List<LMessage> messages, bool doNotGrouping = false) {
            foreach (LMessage msg in messages) {
                Add(msg);
            }
            if (!doNotGrouping) CreateGroup();
        }

        public MessagesCollection(List<Message> messages, bool doNotGrouping = false) {
            foreach (Message msg in messages) {
                Add(new LMessage(msg));
            }
            if (!doNotGrouping) CreateGroup();
        }

        public void Insert(LMessage message) {
            int idx = 0;
            var q = from m in Items where m.ConversationMessageId == message.ConversationMessageId select m;
            if (q.Count() == 1) {
                LMessage old = q.First();
                idx = IndexOf(old);
                Remove(old);
                if (GroupedMessages != null) GroupedMessages.Remove(old);
                Insert(idx, message);
            } else {
                idx = this.ToList().BinarySearch(message);
                if (idx < 0) idx = ~idx;
                Insert(idx, message);
            }
            if (GroupedMessages != null) GroupedMessages.Insert(message);
        }

        public void Remove(LMessage message) {
            base.Remove(message);
            if (GroupedMessages != null) GroupedMessages.Remove(message);
        }

        public void Insert(Message message) {
            Insert(new LMessage(message));
        }

        private void CreateGroup() {
            GroupedMessages = new GroupedMessagesCollection(this);
        }

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e) {
            base.OnCollectionChanged(e);
            if (e.Action == NotifyCollectionChangedAction.Reset) {
                if (GroupedMessages != null) GroupedMessages.Clear();
            }
        }
    }

    public class MessagesCollection<TKey> : MessagesCollection, IComparable {
        public MessagesCollection(TKey key, List<LMessage> messages) : base(messages, true) {
            Key = key;
        }

        public TKey Key { get; private set; }

        public int CompareTo(object obj) {
            if (obj is MessagesCollection<DateTime> msgd && Key is DateTime dt) {
                return dt.CompareTo(msgd.Key);
            }
            throw new InvalidOperationException("No comparable TKey.");
        }
    }

    public class MessagesCollectionGroupItem : MessagesCollection<DateTime> {
        public MessagesCollectionGroupItem(DateTime key, List<LMessage> messages) : base(key, messages) { }
    }
}