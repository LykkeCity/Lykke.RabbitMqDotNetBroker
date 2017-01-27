using System;
using RabbitMQ.Client;

namespace Lykke.RabbitMqBroker.Publisher
{
    public class DefaultFnoutPublishStrategy : IRabbitMqPublishStrategy
    {
        private readonly string _routingKey;

        public DefaultFnoutPublishStrategy(string routingKey = "")
        {
            _routingKey = routingKey;
        }

        public void Configure(RabbitMqSettings settings, IModel channel)
        {
            channel.ExchangeDeclare(exchange: settings.QueueName, type: "fanout");
        }

        public void Publish(RabbitMqSettings settings, IModel channel, byte[] body)
        {
            channel.BasicPublish(exchange: settings.QueueName,
                      routingKey: _routingKey,
                      basicProperties: null,
                      body: body);
        }
    }
}
