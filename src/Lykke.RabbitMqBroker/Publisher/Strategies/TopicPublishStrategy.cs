using System;
using RabbitMQ.Client;

namespace Lykke.RabbitMqBroker.Publisher.Strategies
{
    /// <summary>
    /// Publish strategy for topic exchange.
    /// </summary>
    public sealed class TopicPublishStrategy : IRabbitMqPublishStrategy
    {
        private readonly bool _durable;
        private readonly string _routingKey;

        public TopicPublishStrategy(RabbitMqSubscriptionSettings settings)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            _durable = settings.IsDurable;
            _routingKey = settings.RoutingKey ?? string.Empty;
        }

        public void Configure(RabbitMqSubscriptionSettings settings, IModel channel)
        {
            channel.ExchangeDeclare(exchange: settings.ExchangeName, type: "topic", durable: _durable);
        }

        public void Publish(RabbitMqSubscriptionSettings settings, IModel channel, RawMessage message)
        {
            IBasicProperties basicProperties = null;
            if (message.Headers != null)
            {
                basicProperties = channel.CreateBasicProperties();
                basicProperties.Headers = message.Headers;
            }

            channel.BasicPublish(
                exchange: settings.ExchangeName,
                routingKey: message.RoutingKey ?? _routingKey,
                basicProperties: basicProperties,
                body: message.Body);
        }
    }
}
