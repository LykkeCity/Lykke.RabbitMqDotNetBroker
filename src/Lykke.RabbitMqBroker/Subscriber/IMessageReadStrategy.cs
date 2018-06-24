using JetBrains.Annotations;
using RabbitMQ.Client;

namespace Lykke.RabbitMqBroker.Subscriber
{
    [PublicAPI]
    public interface IMessageReadStrategy
    {
        string Configure(RabbitMqSubscriptionSettings settings, IModel channel);
    }
}
