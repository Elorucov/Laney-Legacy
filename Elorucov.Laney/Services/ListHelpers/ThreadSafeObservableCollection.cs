using Elorucov.Laney.Services.Common;
using Elorucov.Laney.Services.Logger;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;

namespace Elorucov.Laney.Services.ListHelpers {
    public class ThreadSafeObservableCollection<T> : ObservableCollection<T> {
        static bool ths {
            get {
                return AppParameters.ThreadSafety || ViewManagement.GetWindowType() == WindowType.ContactPanel;
            }
        }

        private event NotifyCollectionChangedEventHandler ntshandler;
        private readonly object _lock = new object();
        private readonly Dictionary<SynchronizationContext, NotifyCollectionChangedEventHandler> _handlersWithContext = new Dictionary<SynchronizationContext, NotifyCollectionChangedEventHandler>();
        public override event NotifyCollectionChangedEventHandler CollectionChanged {
            add {
                if (value == null) return;
                if (ths) {
                    var synchronizationContext = SynchronizationContext.Current;
                    lock (_lock) {
                        if (_handlersWithContext.TryGetValue(synchronizationContext, out NotifyCollectionChangedEventHandler eventHandler)) {
                            eventHandler += value;
                            _handlersWithContext[synchronizationContext] = eventHandler;
                        } else {
                            _handlersWithContext.Add(synchronizationContext, value);
                        }
                    }
                } else {
                    ntshandler += value;
                }
            }
            remove {
                if (value == null) return;
                if (ths) {
                    var synchronizationContext = SynchronizationContext.Current;
                    lock (_lock) {
                        if (_handlersWithContext.TryGetValue(synchronizationContext, out NotifyCollectionChangedEventHandler eventHandler)) {
                            eventHandler -= value;
                            if (eventHandler != null) {
                                _handlersWithContext[synchronizationContext] = eventHandler;
                            } else {
                                _handlersWithContext.Remove(synchronizationContext);
                            }
                        }
                    }
                } else {
                    ntshandler -= value;
                }
            }
        }

        public ThreadSafeObservableCollection() { }

        public ThreadSafeObservableCollection(IEnumerable<T> items) : base(items) { }

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e) {
            if (ths) {
                SafeExec(e);
            } else {
                ntshandler?.Invoke(this, e);
            }
        }

        private void SafeExec(NotifyCollectionChangedEventArgs eventArgs) {
            try {
                KeyValuePair<SynchronizationContext, NotifyCollectionChangedEventHandler>[] handlersWithContext;
                lock (_lock) {
                    handlersWithContext = _handlersWithContext.ToArray();
                }
                foreach (var handlerWithContext in handlersWithContext) {
                    var synchronizationContext = handlerWithContext.Key;
                    var eventHandler = handlerWithContext.Value;
                    synchronizationContext.Post(o => eventHandler(this, eventArgs), null);
                }
            } catch (Exception ex) {
                Log.Error($"An error occured in SafeExec! 0x{ex.HResult.ToString("x8")}");
            }
        }

        public void FastMove(int oldIndex, int newIndex) {
            T item = Items[oldIndex];
            RemoveAt(oldIndex);
            Insert(newIndex, item);
        }
    }
}
