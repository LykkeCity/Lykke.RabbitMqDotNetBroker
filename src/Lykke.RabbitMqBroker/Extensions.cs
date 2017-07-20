using Lykke.RabbitMqBroker.Publisher;
using Lykke.RabbitMqBroker.Subscriber;

namespace Lykke.RabbitMqBroker
{
    internal static class RabbitMqSettingsExtension
    {
        internal static string GetPublisherName(this RabbitMqSubscribtionSettings settings)
        {
            return $"Publisher {settings.ExchangeName}";
        }

        internal static string GetSubscriberName(this RabbitMqSubscribtionSettings settings)
        {
            return $"Subscriber {settings.QueueName}";
        }

        internal static string GetQueueOrExchangeName(this RabbitMqSubscribtionSettings settings)
        {
            return $"{(!string.IsNullOrEmpty(settings.ExchangeName) ? $"Exchange: {settings.ExchangeName}, " : string.Empty)}Queue: {settings.QueueName}";
        }
    }
}
