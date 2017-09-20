using System;
using System.Collections.Generic;
using System.Threading;

namespace Lykke.RabbitMqBroker.Publisher
{
    public interface IPublisherBuffer<T> : IEnumerable<T>, IDisposable
    {
        int Count { get; }

        void Enqueue(T message, CancellationToken cancelationToken);

        T Dequeue(CancellationToken cancelationToken);
    }
}