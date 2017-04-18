using System;
using RabbitMQ.Client;

namespace Lykke.RabbitMqBroker.Subscriber
{
    public class MessageReadWithTemporaryQueueStrategy : IMessageReadStrategy
    {
        private readonly string _routingKey;

        public MessageReadWithTemporaryQueueStrategy(string routingKey = "")
        {
            _routingKey = routingKey;
        }

        public string Configure(RabbitMqSubscriberSettings settings, IModel channel)
        {
            // If specified queue name is empty generate random name
            var queueName = String.IsNullOrEmpty(settings.QueueName) 
                ? settings.ExchangeName + "." + Guid.NewGuid().ToString() 
                : settings.QueueName;

            // For random name set autodelete
            var autodelete = String.IsNullOrEmpty(settings.QueueName) ? true : false;

            settings.QueueName = channel.QueueDeclare(queueName, durable: settings.IsDurable, exclusive: false, autoDelete: autodelete).QueueName;

            channel.QueueBind(
                queue: settings.QueueName,
                exchange: settings.ExchangeName,
                routingKey: _routingKey);

            return settings.QueueName;
        }

    }

}
