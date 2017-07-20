using System;
using Lykke.RabbitMqBroker.Subscriber;
using RabbitMQ.Client;

namespace Lykke.RabbitMqBroker.Publisher
{
    public class DefaultFanoutPublishStrategy : IRabbitMqPublishStrategy
    {
        private readonly bool _durable;
        private readonly string _routingKey;

        public DefaultFanoutPublishStrategy()
        {
            _routingKey = "";
        }

        public DefaultFanoutPublishStrategy(RabbitMqSubscribtionSettings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }
            _routingKey = settings.RoutingKey;
            _durable = settings.IsDurable;
        }

        public void Configure(RabbitMqSubscribtionSettings settings, IModel channel)
        {
            channel.ExchangeDeclare(exchange: settings.ExchangeName, type: "fanout", durable: _durable);
            if (!string.IsNullOrEmpty(settings.DeadLetterExchangeName))
            {
                channel.ExchangeDeclare(exchange: settings.DeadLetterExchangeName, type: "direct", durable: true);
            }
        }

        public void Publish(RabbitMqSubscribtionSettings settings, IModel channel, byte[] body)
        {
            channel.BasicPublish(exchange: settings.ExchangeName,
                      routingKey: _routingKey,
                      basicProperties: null,
                      body: body);
        }
    }
}
