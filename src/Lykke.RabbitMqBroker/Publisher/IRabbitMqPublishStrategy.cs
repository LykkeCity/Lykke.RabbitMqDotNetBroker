using Lykke.RabbitMqBroker.Subscriber;
using RabbitMQ.Client;

namespace Lykke.RabbitMqBroker.Publisher
{
    public interface IRabbitMqPublishStrategy
    {
        void Configure(RabbitMqSubscriptionSettings settings, IModel channel);
        void Publish(RabbitMqSubscriptionSettings settings, IModel channel, RawMessage message);
    }
}
