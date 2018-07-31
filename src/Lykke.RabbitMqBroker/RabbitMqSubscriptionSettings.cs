using System;

namespace Lykke.RabbitMqBroker.Subscriber
{
    // TODO: Hide setters, when next breaking changes release will be required
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

        /// <summary>
        /// Creates settings for publisher's endpoint, with convention based exchange name
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="nameOfEndpoint">Endpoint name without "lykke" namespace</param>
        public static RabbitMqSubscriptionSettings CreateForPublisher(string connectionString, string nameOfEndpoint)
        {
            return CreateForPublisher(connectionString, "lykke", nameOfEndpoint);
        }

        /// <summary>
        /// Creates settings for publisher's endpoint, with convention based exchange name
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="namespace">Endpoint's namespace</param>
        /// <param name="nameOfEndpoint">Endpoint's name</param>
        public static RabbitMqSubscriptionSettings CreateForPublisher(string connectionString, string @namespace, string nameOfEndpoint)
        {
            return new RabbitMqSubscriptionSettings
            {
                ConnectionString = connectionString,
                ExchangeName = GetExchangeName(@namespace, nameOfEndpoint)
            };
        }

        /// <summary>
        /// Creates settings for subscriber's endpoint, with convention based exchange, queue and dead letter queue names
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="nameOfSourceEndpoint">Endpoint's name, to which messages subscriber want to be subsribed, without "lykke" namespace</param>
        /// <param name="nameOfEndpoint">Subscribers's endpoint name, without "lykke" namepsace</param>
        public static RabbitMqSubscriptionSettings CreateForSubscriber(string connectionString, 
            string nameOfSourceEndpoint, string nameOfEndpoint)
        {
            return CreateForSubscriber(connectionString, "lykke", nameOfSourceEndpoint, "lykke", nameOfEndpoint);
        }

        /// <summary>
        /// Creates settings for subscriber's endpoint, with convention based exchange, queue and dead letter queue names
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="namespaceOfSourceEndpoint">Endpoint's name, to which messages subscriber want to be subscribed</param>
        /// <param name="nameOfSourceEndpoint">Endpoint's name, to which messages subscriber want to be subsribed</param>
        /// <param name="namespaceOfEndpoint">Subscriber's endpoint namesace</param>
        /// <param name="nameOfEndpoint">Subscribers's endpoint name</param>
        public static RabbitMqSubscriptionSettings CreateForSubscriber(string connectionString, 
            string namespaceOfSourceEndpoint, string nameOfSourceEndpoint, 
            string namespaceOfEndpoint, string nameOfEndpoint)
        {
            return new RabbitMqSubscriptionSettings
            {
                ConnectionString = connectionString,
                ExchangeName = GetExchangeName(namespaceOfSourceEndpoint, nameOfSourceEndpoint),
                QueueName = GetQueueName(namespaceOfSourceEndpoint, nameOfSourceEndpoint, nameOfEndpoint),
                DeadLetterExchangeName = GetDeadLetterExchangeName(namespaceOfEndpoint, nameOfSourceEndpoint, nameOfEndpoint)
            };
        }

        public RabbitMqSubscriptionSettings MakeDurable()
        {
            IsDurable = true;

            return this;
        }

        public RabbitMqSubscriptionSettings UseRoutingKey(string routingKey)
        {
            RoutingKey = routingKey;

            return this;
        }

        public RabbitMqSubscriptionSettings DelayTheRecconectionForA(TimeSpan delay)
        {
            ReconnectionDelay = delay;

            return this;
        }

        public RabbitMqSubscriptionSettings AlarmWhenReconnected(int timesInARow)
        {
            ReconnectionsCountToAlarm = timesInARow;

            return this;
        }

        private static string GetExchangeName(string @namespace, string endpointName)
        {
            return $"{NormalizeName(@namespace)}.{NormalizeName(endpointName)}";
        }

        private static string GetQueueName(string @namespace, string nameOfSourceEndpoint, string endpointName)
        {
            return $"{GetExchangeName(@namespace, nameOfSourceEndpoint)}.{NormalizeName(endpointName)}";
        }

        private static string GetDeadLetterExchangeName(string @namespace, string nameOfSourceEndpoint, string endpointName)
        {
            return $"{NormalizeName(@namespace)}.{NormalizeName(endpointName)}.{NormalizeName(nameOfSourceEndpoint)}.dlx";
        }

        private static string NormalizeName(string appName)
        {
            return appName;
        }
    }
}
