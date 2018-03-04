using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Lykke.RabbitMqBroker.Publisher
{
    internal class InMemoryBuffer : IPublisherBuffer
    {
        private readonly BlockingCollection<RawMessage> _items;

        public InMemoryBuffer()
        {
            _items = new BlockingCollection<RawMessage>();
        }

        public int Count => _items.Count;

        public void Enqueue(RawMessage message, CancellationToken cancelationToken)
        {
            _items.Add(message, cancelationToken);
        }

        public RawMessage Dequeue(CancellationToken cancelationToken)
        {
            return _items.Take(cancelationToken);
        }

        public void Dispose()
        {
            _items.Dispose();
        }

        public IEnumerator<RawMessage> GetEnumerator()
        {
            return ((IEnumerable<RawMessage>)_items).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_items).GetEnumerator();
        }
    }
}
