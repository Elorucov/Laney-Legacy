using Elorucov.Laney.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Elorucov.Laney.Helpers
{
    public class GroupedMessagesCollection : ThreadSafeObservableCollection<MessagesCollection<DateTime>>
    {
        public GroupedMessagesCollection(MessagesCollection messages)
        {
            if (messages != null && messages.Count > 0)
            {
                var groups = messages.GroupBy(m => m.SentDateTime.Date);
                foreach (var g in groups)
                {
                    MessagesCollection<DateTime> mc = new MessagesCollection<DateTime>(g.Key, g.ToList());
                    Add(mc);
                    // Debug.WriteLine($"GroupedMessagesCollection: Key = {g.Key.ToShortDateString()}, Count = {mc.Count}");
                }
            }
        }

        public void Insert(MessageViewModel message)
        {
            var q = from g in Items where g.Key == message.SentDateTime.Date select g;
            if (q.Count() == 1)
            {
                q.First().Insert(message);
                // Debug.WriteLine($"GroupedMessagesCollection: Message {message.Id} inserted in group {q.First().Key.ToShortDateString()}");
            }
            else
            {
                MessagesCollection<DateTime> group = new MessagesCollection<DateTime>(message.SentDateTime.Date, new List<MessageViewModel> { message });
                int idx = this.ToList().BinarySearch(group);
                if (idx < 0) idx = ~idx;
                Insert(idx, group);
                // Debug.WriteLine($"GroupedMessagesCollection: Message {message.Id} inserted in new group (idx: {idx})");
            }
        }

        public void Remove(MessageViewModel message)
        {
            var q = from g in Items where g.Key == message.SentDateTime.Date select g;
            if (q.Count() == 1)
            {
                MessagesCollection<DateTime> i = q.First();
                i.RemoveMessage(message);
                if (i.Count == 0) Remove(i);
                // Debug.WriteLine($"GroupedMessagesCollection: Message {message.Id} removed from group {q.First().Key}");
            }
        }
    }
}