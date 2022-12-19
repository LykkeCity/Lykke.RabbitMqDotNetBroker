namespace Lykke.RabbitMqBroker.Publisher.Strategies
{
    /// <summary>
    /// Publish strategy for fanout exchange with confirmations.
    /// </summary>
    public sealed class FanoutPublishStrategyWithConfirmations : BasicPublishStrategyWithConfirmations
    {
        public FanoutPublishStrategyWithConfirmations(RabbitMqSubscriptionSettings settings)
        : base(settings, RabbitMQ.Client.ExchangeType.Fanout)
        {
        }

        protected override string GetRoutingKey(RawMessage message)
        {
            return string.Empty;
        }
    }
}
