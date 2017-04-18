namespace Lykke.RabbitMqBroker.Subscriber
{
    public class RabbitMqSubscriberSettings
    {
        public string ConnectionString { get; set; }
        public string QueueName { get; set; }
        public string ExchangeName { get; set; }

        public bool IsDurable { get; set; }
    }
}
