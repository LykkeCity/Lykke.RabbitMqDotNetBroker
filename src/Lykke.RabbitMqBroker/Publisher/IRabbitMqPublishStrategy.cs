// Copyright (c) Lykke Corp.
// Licensed under the MIT License. See the LICENSE file in the project root for more information.

using Lykke.RabbitMqBroker.Subscriber;
using RabbitMQ.Client;

namespace Lykke.RabbitMqBroker.Publisher
{
    public interface IRabbitMqPublishStrategy
    {
        void Configure(RabbitMqSubscriptionSettings settings, IModel channel);
        void Publish(RabbitMqSubscriptionSettings settings, IModel channel, RawMessage message);
    }
}
