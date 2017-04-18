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

        public void Configure(RabbitMqPublisherSettings settings, IModel channel)
        {
            channel.ExchangeDeclare(exchange: settings.ExchangeName, type: "fanout", durable: _durable);
        }

        public void Publish(RabbitMqPublisherSettings settings, IModel channel, byte[] body)
        {
            channel.BasicPublish(exchange: settings.ExchangeName,
                      routingKey: _routingKey,
                      basicProperties: null,
                      body: body);
        }
    }
}
