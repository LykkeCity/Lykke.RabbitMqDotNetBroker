using System;

namespace Lykke.RabbitMqBroker.Subscriber
{
    public sealed class RabbitMqSubscriptionSettings
    {
        public string ConnectionString { get; set; }
        public string QueueName { get; set; }
        public string ExchangeName { get; set; }
        public bool IsDurable { get; set; }
        public string DeadLetterExchangeName { get; set; }
        public string RoutingKey { get; set; }
        /// <summary>
        /// Default is 3 seconds
        /// </summary>
        public TimeSpan ReconnectionDelay { get; set; }
        /// <summary>
        /// Count of silent reconnection attempts before write error message to the log. Default is - 20
        /// </summary>
        public int ReconnectionsCountToAlarm { get; set; }

        public RabbitMqSubscriptionSettings()
        {
            ReconnectionDelay = TimeSpan.FromSeconds(3);
            ReconnectionsCountToAlarm = 20;
            RoutingKey = string.Empty;
        }
    }
}
