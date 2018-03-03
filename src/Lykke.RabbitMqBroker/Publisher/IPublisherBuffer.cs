using System;
using System.Collections.Generic;
using System.Threading;

namespace Lykke.RabbitMqBroker.Publisher
{
    internal interface IPublisherBuffer : IEnumerable<RawMessage>, IDisposable
    {
        int Count { get; }

        void Enqueue(RawMessage message, CancellationToken cancelationToken);

        RawMessage Dequeue(CancellationToken cancelationToken);
    }
}
