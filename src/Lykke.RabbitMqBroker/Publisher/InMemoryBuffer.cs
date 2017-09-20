using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Lykke.RabbitMqBroker.Publisher
{
    internal class InMemoryBuffer<T> : IPublisherBuffer<T>
    {
        private readonly BlockingCollection<T> _items;

        public InMemoryBuffer()
        {
            _items = new BlockingCollection<T>();
        }

        public int Count => _items.Count;

        public void Enqueue(T message, CancellationToken cancelationToken)
        {
            _items.Add(message);
        }

        public T Dequeue(CancellationToken cancelationToken)
        {
            return _items.Take(cancelationToken);
        }

        public void Dispose()
        {
            _items.Dispose();
        }

        public IEnumerator<T> GetEnumerator()
        {
            return ((IEnumerable<T>)_items).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_items).GetEnumerator();
        }
    }
}