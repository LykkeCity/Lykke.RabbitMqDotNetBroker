namespace Lykke.RabbitMqBroker
{
    internal static class RabbitMqSettingsExtension
    {
        internal static string GetPublisherName(this RabbitMqSettings settings)
        {
            return $"QueueProducer {settings.QueueName}";
        }

        internal static string GetSubscriberName(this RabbitMqSettings settings)
        {
            return $"RabbitMQ {settings.QueueName}";
        }

        internal static string GetQueueOrExchangeName(this RabbitMqSettings settings)
        {
            return $"{(!string.IsNullOrEmpty(settings.ExchangeName) ? $"Exchange: {settings.ExchangeName}" : string.Empty)}, Queue: {settings.QueueName}";
        }
    }
}
