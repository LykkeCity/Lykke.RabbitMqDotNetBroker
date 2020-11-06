using System;
using Lykke.RabbitMqBroker.Subscriber;
using RabbitMQ.Client;

namespace Lykke.RabbitMqBroker.Publisher
{
    /// <summary>
    /// Publish strategy for fanout exchange with publisher confirmation.
    /// </summary>
    public sealed class FanoutPublishStrategyWithConfirmations : IRabbitMqPublishStrategy
    {
        private readonly bool _durable;

        public FanoutPublishStrategyWithConfirmations(RabbitMqSubscriptionSettings settings)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            _durable = settings.IsDurable;
        }
        
        public void Configure(RabbitMqSubscriptionSettings settings, IModel channel)
        {
            channel.ExchangeDeclare(exchange: settings.ExchangeName, type: "fanout", durable: _durable);
            channel.ConfirmSelect();
        }

        public void Publish(RabbitMqSubscriptionSettings settings, IModel channel, RawMessage message)
        {
            channel.BasicPublish(
                exchange: settings.ExchangeName,
                routingKey: string.Empty,
                basicProperties: null,
                body: message.Body);
            channel.WaitForConfirmsOrDie(settings.PublisherConfirmationTimeout);
        }
    }
}
