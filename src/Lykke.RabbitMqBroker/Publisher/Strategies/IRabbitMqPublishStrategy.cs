// Copyright (c) Lykke Corp.
// Licensed under the MIT License. See the LICENSE file in the project root for more information.

using RabbitMQ.Client;

namespace Lykke.RabbitMqBroker.Publisher.Strategies
{
    public interface IRabbitMqPublishStrategy
    {
        void Configure(IModel channel);
        void Publish(IModel channel, RawMessage message);
    }
}
