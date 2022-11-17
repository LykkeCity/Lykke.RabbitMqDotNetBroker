using System;
using RabbitMQ.Client;

namespace Lykke.RabbitMqBroker.Publisher.Strategies
{
    public abstract class BasicPublishStrategyWithConfirmations : BasicPublishStrategy
    {
        private readonly TimeSpan _defaultConfirmationTimeout = TimeSpan.FromSeconds(5);

        internal BasicPublishStrategyWithConfirmations(RabbitMqSubscriptionSettings settings, string exchangeType)
        : base(settings, exchangeType)
        {
        }

        public override void Configure(RabbitMqSubscriptionSettings settings, IModel channel)
        {
            base.Configure(settings, channel);
            channel.ConfirmSelect();
        }

        public override void Publish(RabbitMqSubscriptionSettings settings, IModel channel, RawMessage message)
        {
            base.Publish(settings, channel, message);
            channel.WaitForConfirmsOrDie(settings.PublisherConfirmationTimeout ?? _defaultConfirmationTimeout);
        }
    }
}
