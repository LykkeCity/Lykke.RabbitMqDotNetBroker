using System;
using RabbitMQ.Client;

namespace Lykke.RabbitMqBroker.Publisher.Strategies
{
    public abstract class BasicPublishStrategy : IRabbitMqPublishStrategy
    {
        protected readonly string ExchangeType;
        protected readonly bool Durable;
        protected readonly string RoutingKey;

        internal BasicPublishStrategy(RabbitMqSubscriptionSettings settings, string exchangeType)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));
            ExchangeType = exchangeType;

            Durable = settings.IsDurable;
            RoutingKey = settings.RoutingKey ?? string.Empty;
        }

        public virtual void Configure(RabbitMqSubscriptionSettings settings, IModel channel)
        {
            channel.ExchangeDeclare(exchange: settings.ExchangeName, type: ExchangeType, durable: Durable);
        }

        public virtual void Publish(RabbitMqSubscriptionSettings settings, IModel channel, RawMessage message)
        {
            IBasicProperties basicProperties = null;
            if (message.Headers != null)
            {
                basicProperties = channel.CreateBasicProperties();
                basicProperties.Headers = message.Headers;
            }

            channel.BasicPublish(
                exchange: settings.ExchangeName,
                routingKey: GetRoutingKey(message),
                basicProperties: basicProperties,
                body: message.Body);
        }

        protected virtual string GetRoutingKey(RawMessage message)
        {
            return message.RoutingKey ?? RoutingKey;
        }
    }
}
