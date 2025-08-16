using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;

namespace Elorucov.Laney.Helpers
{
    public class ThreadSafeObservableCollection<T> : ObservableCollection<T>
    {
        private readonly object _lock = new object();
        private readonly Dictionary<SynchronizationContext, NotifyCollectionChangedEventHandler> _handlersWithContext = new Dictionary<SynchronizationContext, NotifyCollectionChangedEventHandler>();
        public override event NotifyCollectionChangedEventHandler CollectionChanged
        {
            add
            {
                if (value == null)
                {
                    return;
                }
                var synchronizationContext = SynchronizationContext.Current;
                lock (_lock)
                {
                    if (_handlersWithContext.TryGetValue(synchronizationContext, out NotifyCollectionChangedEventHandler eventHandler))
                    {
                        eventHandler += value;
                        _handlersWithContext[synchronizationContext] = eventHandler;
                    }
                    else
                    {
                        _handlersWithContext.Add(synchronizationContext, value);
                    }
                }
            }
            remove
            {
                if (value == null)
                {
                    return;
                }
                var synchronizationContext = SynchronizationContext.Current;
                lock (_lock)
                {
                    if (_handlersWithContext.TryGetValue(synchronizationContext, out NotifyCollectionChangedEventHandler eventHandler))
                    {
                        eventHandler -= value;
                        if (eventHandler != null)
                        {
                            _handlersWithContext[synchronizationContext] = eventHandler;
                        }
                        else
                        {
                            _handlersWithContext.Remove(synchronizationContext);
                        }
                    }
                }
            }
        }

        public ThreadSafeObservableCollection() { }

        public ThreadSafeObservableCollection(IEnumerable<T> items) : base(items) { }

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            SafeExec(e);
        }

        private void SafeExec(NotifyCollectionChangedEventArgs eventArgs)
        {
            KeyValuePair<SynchronizationContext, NotifyCollectionChangedEventHandler>[] handlersWithContext;
            lock (_lock)
            {
                handlersWithContext = _handlersWithContext.ToArray();
            }
            foreach (var handlerWithContext in handlersWithContext)
            {
                var synchronizationContext = handlerWithContext.Key;
                var eventHandler = handlerWithContext.Value;
                synchronizationContext.Post(o => eventHandler(this, eventArgs), null);
            }
        }

        public void FastMove(int oldIndex, int newIndex)
        {
            T item = Items[oldIndex];
            lock (this)
            {
                RemoveAt(oldIndex);
                Insert(newIndex, item);
            }
        }
    }
}