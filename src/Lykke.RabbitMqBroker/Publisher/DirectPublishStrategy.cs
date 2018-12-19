// Copyright (c) Lykke Corp.
// Licensed under the MIT License. See the LICENSE file in the project root for more information.

using System;
using Lykke.RabbitMqBroker.Subscriber;
using RabbitMQ.Client;

namespace Lykke.RabbitMqBroker.Publisher
{
    /// <summary>
    /// Publish strategy for direct exchange.
    /// </summary>
    public sealed class DirectPublishStrategy : IRabbitMqPublishStrategy
    {
        private readonly bool _durable;
        private readonly string _routingKey;

        public DirectPublishStrategy(RabbitMqSubscriptionSettings settings)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            _durable = settings.IsDurable;
            _routingKey = settings.RoutingKey ?? string.Empty;
        }

        public void Configure(RabbitMqSubscriptionSettings settings, IModel channel)
        {
            channel.ExchangeDeclare(exchange: settings.ExchangeName, type: "direct", durable: _durable);
        }

        public void Publish(RabbitMqSubscriptionSettings settings, IModel channel, RawMessage message)
        {
            channel.BasicPublish(
                exchange: settings.ExchangeName,
                routingKey: message.RoutingKey ?? _routingKey,
                basicProperties: null,
                body: message.Body);
        }
    }
}
