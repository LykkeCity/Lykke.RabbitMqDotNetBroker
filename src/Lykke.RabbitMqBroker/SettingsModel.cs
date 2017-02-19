namespace Lykke.RabbitMqBroker
{
    public class RabbitMqSettings
    {
        public string ConnectionString { get; set; }
        public string QueueName { get; set; }
        public string ExchangeName { get; set; }
    }
}
