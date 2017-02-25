namespace Lykke.RabbitMqBroker
{
    internal static class BrokerUtils
    {
        internal static string GetPublisherName(string queue)
        {
            return $"QueueProducer {queue}";
        }

        internal static string GetSubscriberName(string queue)
        {
            return $"RabbitMQ {queue}";
        }

        internal static string GetQueueOrExchangeName(string exchange, string queue)
        {
            return $"{(!string.IsNullOrEmpty(exchange) ? $"Exchange: {exchange}" : string.Empty)}, Queue: {queue}";
        }
    }
}
