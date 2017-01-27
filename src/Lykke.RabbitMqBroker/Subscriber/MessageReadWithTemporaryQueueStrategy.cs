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

        public string Configure(RabbitMqSettings settings, IModel channel)
        {
            var queueName = channel.QueueDeclare().QueueName;

            channel.QueueBind(
                queue: queueName,
                exchange: settings.QueueName,
                routingKey: _routingKey);

            return queueName;
        }
    }
}
