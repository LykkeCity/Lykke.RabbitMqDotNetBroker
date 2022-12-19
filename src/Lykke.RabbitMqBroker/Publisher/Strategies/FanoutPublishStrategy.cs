namespace Lykke.RabbitMqBroker.Publisher.Strategies
{
    /// <summary>
    /// Publish strategy for fanout exchange.
    /// </summary>
    public sealed class FanoutPublishStrategy : BasicPublishStrategy
    {
        public FanoutPublishStrategy(RabbitMqSubscriptionSettings settings)
            : base(settings, RabbitMQ.Client.ExchangeType.Fanout)
        {
        }

        protected override string GetRoutingKey(RawMessage message)
        {
            return string.Empty;
        }
    }
}
