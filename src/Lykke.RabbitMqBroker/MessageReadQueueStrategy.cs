using System;
using RabbitMQ.Client;

namespace Lykke.RabbitMqBroker
{
    public class MessageReadQueueStrategy : IMessageReadStrategy
    {
        public string Configure(RabbitMqSettings settings, IModel channel)
        {
            return settings.QueueName;
        }
    }
}
