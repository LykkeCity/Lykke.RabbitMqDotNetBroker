using RabbitMQ.Client;

namespace Lykke.RabbitMqBroker.Subscriber
{
    public class MessageReadQueueStrategy : IMessageReadStrategy
    {
        public string Configure(RabbitMqSettings settings, IModel channel)
        {
            return settings.QueueName;
        }
    }
}
