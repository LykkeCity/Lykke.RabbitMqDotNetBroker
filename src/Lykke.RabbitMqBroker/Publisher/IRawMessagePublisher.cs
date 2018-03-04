using System;
using System.Collections.Generic;

namespace Lykke.RabbitMqBroker.Publisher
{
    internal interface IRawMessagePublisher : IDisposable
    {
        string Name { get; }
        int BufferedMessagesCount { get; }
        void Produce(RawMessage message);
        IReadOnlyList<RawMessage> GetBufferedMessages();
        void Stop();
    }
}
