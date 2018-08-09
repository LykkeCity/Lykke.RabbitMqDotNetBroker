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
            return $"Subscriber {settings.QueueName}{(!string.IsNullOrWhiteSpace(settings.RoutingKey) ? $"/{settings.RoutingKey}" : string.Empty)}";
        }

        internal static string GetQueueOrExchangeName(this RabbitMqSubscriptionSettings settings)
        {
            return $"Exchange: {settings.ExchangeName}{(!string.IsNullOrEmpty(settings.QueueName) ? $" Queue: {settings.QueueName}" : string.Empty)}";
        }
    }
}
