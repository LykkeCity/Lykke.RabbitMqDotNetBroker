namespace Lykke.RabbitMqBroker.Publisher.Strategies
{
    /// <summary>
    /// Publish strategy for topic exchange with confirmations.
    /// </summary>
    public sealed class TopicPublishStrategyWithConfirmations : BasicPublishStrategyWithConfirmations
    {
        public TopicPublishStrategyWithConfirmations(RabbitMqSubscriptionSettings settings)
            : base(settings, RabbitMQ.Client.ExchangeType.Topic)
        {
        }
    }
}
