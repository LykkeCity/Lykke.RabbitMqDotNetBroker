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

            if (settings.QueueName == null)
                settings.QueueName = channel.QueueDeclare(settings.QueueName, settings.IsDurable).QueueName;

            channel.QueueBind(
                queue: settings.QueueName,
                exchange: settings.ExchangeName,
                routingKey: _routingKey);

            return settings.QueueName;
        }

    }

}
