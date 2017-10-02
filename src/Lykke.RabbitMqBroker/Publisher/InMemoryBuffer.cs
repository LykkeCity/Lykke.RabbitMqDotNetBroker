using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Lykke.RabbitMqBroker.Publisher
{
    internal class InMemoryBuffer : IPublisherBuffer
    {
        private readonly BlockingCollection<byte[]> _items;

        public InMemoryBuffer()
        {
            _items = new BlockingCollection<byte[]>();
        }

        public int Count => _items.Count;

        public void Enqueue(byte[] message, CancellationToken cancelationToken)
        {
            _items.Add(message);
        }

        public byte[] Dequeue(CancellationToken cancelationToken)
        {
            return _items.Take(cancelationToken);
        }

        public void Dispose()
        {
            _items.Dispose();
        }

        public IEnumerator<byte[]> GetEnumerator()
        {
            return ((IEnumerable<byte[]>)_items).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_items).GetEnumerator();
        }
    }
}