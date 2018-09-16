// Copyright (c) Lykke Corp.
// Licensed under the MIT License. See the LICENSE file in the project root for more information.

using System;
using Lykke.RabbitMqBroker.Subscriber;
using RabbitMQ.Client;

namespace Lykke.RabbitMqBroker.Publisher
{
    public sealed class DefaultFanoutPublishStrategy : IRabbitMqPublishStrategy
    {
        private readonly bool _durable;
        private readonly string _routingKey;

        public DefaultFanoutPublishStrategy(RabbitMqSubscriptionSettings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }
            _routingKey = settings.RoutingKey ?? string.Empty;
            _durable = settings.IsDurable;
        }

        public void Configure(RabbitMqSubscriptionSettings settings, IModel channel)
        {
            channel.ExchangeDeclare(exchange: settings.ExchangeName, type: "fanout", durable: _durable);
        }

        public void Publish(RabbitMqSubscriptionSettings settings, IModel channel, RawMessage message)
        {
            channel.BasicPublish(exchange: settings.ExchangeName,
                      routingKey: message.RoutingKey ?? _routingKey,
                      basicProperties: null,
                      body: message.Body);
        }
    }
}
