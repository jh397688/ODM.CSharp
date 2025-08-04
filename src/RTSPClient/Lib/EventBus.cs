using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RTSPStream.Lib
{
    public class EventBus
    {
        private readonly ConcurrentDictionary<Type, List<Delegate>> _handlers = new ConcurrentDictionary<Type, List<Delegate>>();

        public IDisposable Subscribe<T>(Action<T> handler)
        {
            var list = _handlers.GetOrAdd(typeof(T), _ => new List<Delegate>());
            lock (list)
            {
                list.Add(handler);
            }

            return new Unsubscriber<T>(list, handler);
        }

        public void Publish<T>(T @event)
        {
            if (!_handlers.TryGetValue(typeof(T), out var list))
                return;

            List<Delegate> snapshot;

            lock (list)
            {
                snapshot = new List<Delegate>(list);
            }

            foreach (Action<T> handler in snapshot)
            {
                try
                {
                    handler(@event);
                }
                catch
                {
                    // Logging
                }
            }
        }

        private class Unsubscriber<T> : IDisposable
        {
            private readonly List<Delegate> _list;
            private readonly Action<T> _handler;

            public Unsubscriber(List<Delegate> list, Action<T> handler)
            {
                _list = list;
                _handler = handler;
            }

            public void Dispose()
            {
                lock (_list)
                {
                    _list.Remove(_handler);
                }
            }
        }
    }
}
