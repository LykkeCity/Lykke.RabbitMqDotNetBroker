using System;
using RabbitMQ.Client;

namespace Lykke.RabbitMqBroker.Publisher.Strategies
{
    public sealed class FanoutPublishStrategyWithConfirmations : IRabbitMqPublishStrategy
    {
        private readonly bool _durable;
        private readonly TimeSpan _defaultConfirmationTimeout = TimeSpan.FromSeconds(5);

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
            channel.WaitForConfirmsOrDie(settings.PublisherConfirmationTimeout ?? _defaultConfirmationTimeout);
        }
    }
}
