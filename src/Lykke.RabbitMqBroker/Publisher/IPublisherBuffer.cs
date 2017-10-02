using System;
using System.Collections.Generic;
using System.Threading;

namespace Lykke.RabbitMqBroker.Publisher
{
    public interface IPublisherBuffer : IEnumerable<byte[]>, IDisposable
    {
        int Count { get; }

        void Enqueue(byte[] message, CancellationToken cancelationToken);

        byte[] Dequeue(CancellationToken cancelationToken);
    }
}