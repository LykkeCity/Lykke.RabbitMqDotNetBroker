using Lykke.RabbitMqBroker.Publisher;
using Lykke.RabbitMqBroker.Subscriber;

namespace Lykke.RabbitMqBroker
{
    internal static class RabbitMqSettingsExtension
    {
        internal static string GetPublisherName(this RabbitMqPublisherSettings settings)
        {
            return $"Publisher {settings.ExchangeName}";
        }

        internal static string GetSubscriberName(this RabbitMqSubscriberSettings settings)
        {
            return $"Subscriber {settings.QueueName}";
        }

        internal static string GetQueueOrExchangeName(this RabbitMqPublisherSettings settings)
        {
            return $"{(!string.IsNullOrEmpty(settings.ExchangeName) ? $"Exchange: {settings.ExchangeName}, " : string.Empty)}";
        }

        internal static string GetQueueOrExchangeName(this RabbitMqSubscriberSettings settings)
        {
            return $"{(!string.IsNullOrEmpty(settings.ExchangeName) ? $"Exchange: {settings.ExchangeName}, " : string.Empty)}Queue: {settings.QueueName}";
        }
    }
}
