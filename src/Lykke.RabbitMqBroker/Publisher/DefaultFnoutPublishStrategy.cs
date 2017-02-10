using System;
using RabbitMQ.Client;

namespace Lykke.RabbitMqBroker.Publisher
{
    public class DefaultFnoutPublishStrategy : IRabbitMqPublishStrategy
    {
        private readonly bool _durable;
        private readonly string _routingKey;

        public DefaultFnoutPublishStrategy(string routingKey = "", bool durable = false)
        {
            _routingKey = routingKey;
            _durable = durable;
        }

        public void Configure(RabbitMqSettings settings, IModel channel)
        {
            channel.ExchangeDeclare(exchange: settings.QueueName, type: "fanout", durable: _durable);
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
