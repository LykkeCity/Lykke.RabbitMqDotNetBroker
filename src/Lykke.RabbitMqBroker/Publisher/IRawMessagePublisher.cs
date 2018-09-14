// Copyright (c) Lykke Corp.
// Licensed under the MIT License. See the LICENSE file in the project root for more information.

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
