using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Windows.System.Threading;

namespace Elorucov.Laney.Helpers
{
    public class Elapser<T>
    {
        private SynchronizationContext _context;

        private Dictionary<T, ThreadPoolTimer> registeredObjects = new Dictionary<T, ThreadPoolTimer>();
        public IReadOnlyList<T> RegisteredObjects { get { return registeredObjects.Keys.ToList(); } }

        public event EventHandler<T> Elapsed;

        public Elapser(SynchronizationContext context = null)
        {
            _context = context == null ? SynchronizationContext.Current : context;
        }

        public void Add(T obj, double milliseconds)
        {
            ThreadPoolTimer timer = ThreadPoolTimer.CreateTimer((u) =>
            {
                _context.Post(o => Elapsed?.Invoke(this, obj), null);
                registeredObjects.Remove(obj);
            }, TimeSpan.FromMilliseconds(milliseconds));

            if (registeredObjects.ContainsKey(obj))
            {
                registeredObjects[obj].Cancel();
                registeredObjects[obj] = timer;
            }
            else
            {
                registeredObjects.Add(obj, timer);
            }
        }

        public void Remove(T obj)
        {
            if (registeredObjects.ContainsKey(obj))
            {
                registeredObjects[obj].Cancel();
                registeredObjects.Remove(obj);
                Elapsed?.Invoke(this, obj);
            }
        }

        public void Clear()
        {
            foreach (var obj in registeredObjects)
            {
                obj.Value.Cancel();
                Elapsed?.Invoke(this, obj.Key);
            }
            registeredObjects.Clear();
        }
    }
}
