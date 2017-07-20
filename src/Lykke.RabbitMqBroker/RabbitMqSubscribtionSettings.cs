namespace Lykke.RabbitMqBroker.Subscriber
{
    public sealed class RabbitMqSubscribtionSettings
    {
        public string ConnectionString { get; set; }
        public string QueueName { get; set; }
        public string ExchangeName { get; set; }
        public bool IsDurable { get; set; }
        public string DeadLetterExchangeName { get; set; }
        public string RoutingKey { get; set; }

    }
}
