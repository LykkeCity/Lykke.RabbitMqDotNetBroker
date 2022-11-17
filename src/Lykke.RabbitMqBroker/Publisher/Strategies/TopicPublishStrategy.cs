namespace Lykke.RabbitMqBroker.Publisher.Strategies
{
    /// <summary>
    /// Publish strategy for topic exchange.
    /// </summary>
    public sealed class TopicPublishStrategy : BasicPublishStrategy
    {
        public TopicPublishStrategy(RabbitMqSubscriptionSettings settings)
            : base(settings, RabbitMQ.Client.ExchangeType.Topic)
        {
        }
    }
}
