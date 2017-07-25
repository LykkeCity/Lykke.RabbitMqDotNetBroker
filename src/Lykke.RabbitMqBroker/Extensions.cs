using Lykke.RabbitMqBroker.Publisher;
using Lykke.RabbitMqBroker.Subscriber;

namespace Lykke.RabbitMqBroker
{
    internal static class RabbitMqSettingsExtension
    {
        internal static string GetPublisherName(this RabbitMqSubscriptionSettings settings)
        {
            return $"Publisher {settings.ExchangeName}";
        }

        internal static string GetSubscriberName(this RabbitMqSubscriptionSettings settings)
        {
            return $"Subscriber {settings.QueueName}";
        }

        internal static string GetQueueOrExchangeName(this RabbitMqSubscriptionSettings settings)
        {
            return $"{(!string.IsNullOrEmpty(settings.ExchangeName) ? $"Exchange: {settings.ExchangeName}, " : string.Empty)}Queue: {settings.QueueName}";
        }
    }
}
