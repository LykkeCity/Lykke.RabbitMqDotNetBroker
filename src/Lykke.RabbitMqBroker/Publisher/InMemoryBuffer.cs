// Copyright (c) Lykke Corp.
// Licensed under the MIT License. See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Lykke.RabbitMqBroker.Publisher
{
    internal class InMemoryBuffer : IPublisherBuffer
    {
        private readonly BlockingCollection<RawMessage> _items;
        private bool _disposed;

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
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed || !disposing)
                return; 
            
            _items.Dispose();
            
            _disposed = true;
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
