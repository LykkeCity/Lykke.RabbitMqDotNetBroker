using RabbitMQ.Client;

namespace Lykke.RabbitMqBroker.Subscriber
{
    public class MessageReadQueueStrategy : IMessageReadStrategy
    {
        public string Configure(RabbitMqSubscriberSettings settings, IModel channel)
        {
            return settings.QueueName;
        }
    }
}
