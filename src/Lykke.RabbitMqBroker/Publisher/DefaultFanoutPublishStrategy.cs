// Copyright (c) Lykke Corp.
// Licensed under the MIT License. See the LICENSE file in the project root for more information.

using System;
using Lykke.RabbitMqBroker.Subscriber;
using RabbitMQ.Client;

namespace Lykke.RabbitMqBroker.Publisher
{
    /// <summary>
    /// Publish strategy for fanout exchange.
    /// </summary>
    public sealed class DefaultFanoutPublishStrategy : IRabbitMqPublishStrategy
    {
        private readonly bool _durable;

        public DefaultFanoutPublishStrategy(RabbitMqSubscriptionSettings settings)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            _durable = settings.IsDurable;
        }

        public void Configure(RabbitMqSubscriptionSettings settings, IModel channel)
        {
            channel.ExchangeDeclare(exchange: settings.ExchangeName, type: "fanout", durable: _durable);
        }

        public void Publish(RabbitMqSubscriptionSettings settings, IModel channel, RawMessage message)
        {
            channel.BasicPublish(
                exchange: settings.ExchangeName,
                routingKey: null,
                basicProperties: null,
                body: message.Body);
        }
    }
}
