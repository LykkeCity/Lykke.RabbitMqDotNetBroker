// Copyright (c) Lykke Corp.
// Licensed under the MIT License. See the LICENSE file in the project root for more information.

using System;
using RabbitMQ.Client;

namespace Lykke.RabbitMqBroker.Publisher.Strategies
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
            IBasicProperties basicProperties = null;
            if (message.Headers != null)
            {
                var props = channel.CreateBasicProperties();
                props.Headers = message.Headers;
            }

            channel.BasicPublish(
                exchange: settings.ExchangeName,
                // routingKey can't be null - I consider this as a bug in RabbitMQ.Client
                routingKey: string.Empty,
                basicProperties: basicProperties,
                body: message.Body);
        }
    }
}
