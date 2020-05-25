// Copyright (c) Lykke Corp.
// Licensed under the MIT License. See the LICENSE file in the project root for more information.

using JetBrains.Annotations;
using RabbitMQ.Client;

namespace Lykke.RabbitMqBroker.Subscriber.Strategies
{
    [PublicAPI]
    public interface IMessageReadStrategy
    {
        string Configure(RabbitMqSubscriptionSettings settings, IModel channel);
    }
}
