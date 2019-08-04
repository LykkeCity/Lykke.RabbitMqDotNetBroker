// Copyright (c) Lykke Corp.
// Licensed under the MIT License. See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using JetBrains.Annotations;

namespace Lykke.RabbitMqBroker.Publisher
{
    public sealed class InMemoryBuffer : IPublisherBuffer
    {
        private readonly ConcurrentQueue<RawMessage> _items;
        private readonly AutoResetEvent _publishLock;
        private bool _disposed;
        
        public InMemoryBuffer()
        {
            _publishLock = new AutoResetEvent(false);
            _items = new ConcurrentQueue<RawMessage>();
        }

        public int Count => _items.Count;

        public void Enqueue(RawMessage message, CancellationToken cancelationToken)
        {
            _items.Enqueue(message);
            _publishLock.Set();
        }

        public void Dequeue(CancellationToken cancelationToken)
        {
            _items.TryDequeue(out _);
        }

        [CanBeNull]
        public RawMessage WaitOneAndPeek(CancellationToken cancelationToken)
        {
            if (_items.Count > 0 || _publishLock.WaitOne())
            {
                if (_items.TryPeek(out var result))
                {
                    return result;
                }
            }
            
            return null;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        
        private void Dispose(bool disposing)
        {
            if (_disposed || !disposing)
                return; 
            
            _publishLock?.Dispose();
            
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
