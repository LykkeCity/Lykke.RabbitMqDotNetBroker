// Copyright (c) Lykke Corp.
// Licensed under the MIT License. See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;

namespace Lykke.RabbitMqBroker.Publisher
{
    internal interface IPublisherBuffer : IEnumerable<RawMessage>, IDisposable
    {
        int Count { get; }

        void Enqueue(RawMessage message, CancellationToken cancelationToken);

        void Dequeue(CancellationToken cancelationToken);

        RawMessage WaitOneAndPeek(CancellationToken cancelationToken);
    }
}
