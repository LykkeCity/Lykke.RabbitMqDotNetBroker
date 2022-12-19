using System;
using RabbitMQ.Client;

namespace Lykke.RabbitMqBroker.Publisher.Strategies
{
    public abstract class BasicPublishStrategy : IRabbitMqPublishStrategy
    {
        protected readonly RabbitMqSubscriptionSettings Settings;
        protected readonly string ExchangeType;

        internal BasicPublishStrategy(RabbitMqSubscriptionSettings settings, string exchangeType)
        {
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            ExchangeType = exchangeType;
        }

        public virtual void Configure(IModel channel)
        {
            channel.ExchangeDeclare(exchange: Settings.ExchangeName, type: ExchangeType, durable: Settings.IsDurable);
        }

        public virtual void Publish(IModel channel, RawMessage message)
        {
            IBasicProperties basicProperties = null;
            if (message.Headers != null)
            {
                basicProperties = channel.CreateBasicProperties();
                basicProperties.Headers = message.Headers;
            }

            channel.BasicPublish(
                exchange: Settings.ExchangeName,
                routingKey: GetRoutingKey(message),
                basicProperties: basicProperties,
                body: message.Body);
        }

        protected virtual string GetRoutingKey(RawMessage message)
        {
            return message.RoutingKey ?? Settings.RoutingKey ?? string.Empty;
        }
    }
}
