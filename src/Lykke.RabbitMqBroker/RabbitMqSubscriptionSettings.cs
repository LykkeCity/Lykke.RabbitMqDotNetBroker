using System;
using System.Text;

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
        /// <param name="endpointName">Endpoint name without "lykke" namespace</param>
        public RabbitMqSubscriptionSettings CreateForPublisher(string connectionString, string endpointName)
        {
            return new RabbitMqSubscriptionSettings
            {
                ConnectionString = connectionString,
                ExchangeName = GetExchangeName("lykke", endpointName)
            };
        }

        /// <summary>
        /// Creates settings for subscriber's endpoint, with convention based exchange, queue and dead letter queue names
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="nameOfSourceEndpoint">Endpoint name, to which message subscriber want to be subsribed, without "lykke" namespace</param>
        /// <param n    ame="endpointName">Subscribers's endpoint name, without "lykke" namepsace</param>
        public static RabbitMqSubscriptionSettings CreateForSubscriber(string connectionString, string nameOfSourceEndpoint, string endpointName)
        {

            return new RabbitMqSubscriptionSettings
            {
                ConnectionString = connectionString,
                ExchangeName = GetExchangeName("lykke", nameOfSourceEndpoint),
                QueueName = GetQueueName("lykke", nameOfSourceEndpoint, endpointName),
                DeadLetterExchangeName = GetDeadLetterExchangeName("lykke", nameOfSourceEndpoint, endpointName)
            };
        }

        public static string GetExchangeName(string @namespace, string endpointName)
        {
            return $"{NormalizeName(@namespace)}.{NormalizeName(endpointName)}";
        }

        public static string GetQueueName(string @namespace, string nameOfSourceEndpoint, string endpointName)
        {
            return $"{GetExchangeName(@namespace, nameOfSourceEndpoint)}.{NormalizeName(endpointName)}";
        }

        public static string GetDeadLetterExchangeName(string @namespace, string nameOfSourceEndpoint, string endpointName)
        {
            return $"{NormalizeName(@namespace)}.{NormalizeName(endpointName)}.{NormalizeName(nameOfSourceEndpoint)}.dlx";
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

        private static string NormalizeName(string appName)
        {
            var sb = new StringBuilder();

            foreach (var c in appName)
            {
                if (char.IsLetterOrDigit(c))
                {
                    sb.Append(c);
                }
            }
               
            return sb.ToString().ToLower();
        }
    }
}
