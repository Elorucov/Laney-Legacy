using Elorucov.Laney.Services.Common;
using Elorucov.Laney.Services.Logger;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Elorucov.Laney.ViewModel {
    public abstract class BaseViewModel : INotifyPropertyChanged {
        static bool ths {
            get {
                return AppParameters.ThreadSafety;
            }
        }

        private event PropertyChangedEventHandler ntshandler;
        private readonly object _lock = new object();
        private readonly Dictionary<SynchronizationContext, PropertyChangedEventHandler> _handlersWithContext = new Dictionary<SynchronizationContext, PropertyChangedEventHandler>();
        public event PropertyChangedEventHandler PropertyChanged {
            add {
                if (value == null) return;
                if (ths) {
                    var synchronizationContext = SynchronizationContext.Current;
                    lock (_lock) {
                        if (synchronizationContext != null && _handlersWithContext.TryGetValue(synchronizationContext, out PropertyChangedEventHandler eventHandler)) {
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
                        if (_handlersWithContext.TryGetValue(synchronizationContext, out PropertyChangedEventHandler eventHandler)) {
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

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            if (ths) {
                KeyValuePair<SynchronizationContext, PropertyChangedEventHandler>[] handlersWithContext;
                lock (_lock) {
                    handlersWithContext = _handlersWithContext.ToArray();
                }
                var eventArgs = new PropertyChangedEventArgs(propertyName);
                foreach (var handlerWithContext in handlersWithContext) {
                    var synchronizationContext = handlerWithContext.Key;
                    var eventHandler = handlerWithContext.Value;
                    synchronizationContext.Post(o => {
                        try {
                            eventHandler(this, eventArgs);
                        } catch (Exception ex) {
                            Log.Error($"Error in BaseViewModel.OnPropertyChanged. HR: 0x{ex.HResult.ToString("x8")}: {ex.Message}");
                        }
                    }, null);
                }
            } else {
                ntshandler?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}