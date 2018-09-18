// Copyright (c) Lykke Corp.
// Licensed under the MIT License. See the LICENSE file in the project root for more information.

using JetBrains.Annotations;
using System;

namespace Lykke.RabbitMqBroker.Subscriber
{
    // TODO: Hide setters, when next breaking changes release will be required
    [PublicAPI]
    public sealed class RabbitMqSubscriptionSettings
    {
        internal const string LykkeNameSpace = "lykke";
        internal static TimeSpan DefaultReconnectionDelay = TimeSpan.FromSeconds(3);
        internal const int DefaultReconnectionsCountToAlarm = 20;

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
            ReconnectionDelay = DefaultReconnectionDelay;
            ReconnectionsCountToAlarm = DefaultReconnectionsCountToAlarm;
            RoutingKey = string.Empty;
        }

        /// <summary>
        /// Creates settings for publisher's endpoint, with convention based exchange name
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="nameOfEndpoint">Endpoint name without "lykke" namespace</param>
        [Obsolete("Use ForPublisher method to avoid lykke.lykke duplication in echange name")]
        public static RabbitMqSubscriptionSettings CreateForPublisher(string connectionString, string nameOfEndpoint)
        {
            return CreateForPublisher(
                connectionString,
                LykkeNameSpace,
                nameOfEndpoint);
        }

        /// <summary>
        /// Creates settings for publisher's endpoint, with convention based exchange name
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="namespace">Endpoint's namespace</param>
        /// <param name="nameOfEndpoint">Endpoint's name</param>
        [Obsolete("Use ForPublisher method to avoid lykke.lykke duplication in echange name")]
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
        [Obsolete("Use ForSubscriber method to avoid lykke.lykke duplication in echange name")]
        public static RabbitMqSubscriptionSettings CreateForSubscriber(
            string connectionString,
            string nameOfSourceEndpoint,
            string nameOfEndpoint)
        {
            return CreateForSubscriber(
                connectionString,
                LykkeNameSpace,
                nameOfSourceEndpoint,
                LykkeNameSpace,
                nameOfEndpoint);
        }

        /// <summary>
        /// Creates settings for subscriber's endpoint, with convention based exchange, queue and dead letter queue names
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="namespaceOfSourceEndpoint">Endpoint's name, to which messages subscriber want to be subscribed</param>
        /// <param name="nameOfSourceEndpoint">Endpoint's name, to which messages subscriber want to be subsribed</param>
        /// <param name="namespaceOfEndpoint">Subscriber's endpoint namesace</param>
        /// <param name="nameOfEndpoint">Subscribers's endpoint name</param>
        [Obsolete("Use ForSubscriber method to avoid lykke.lykke duplication in echange name")]
        public static RabbitMqSubscriptionSettings CreateForSubscriber(
            string connectionString,
            string namespaceOfSourceEndpoint,
            string nameOfSourceEndpoint,
            string namespaceOfEndpoint,
            string nameOfEndpoint)
        {
            return new RabbitMqSubscriptionSettings
            {
                ConnectionString = connectionString,
                ExchangeName = GetExchangeName(namespaceOfSourceEndpoint, nameOfSourceEndpoint),
                QueueName = GetQueueName(namespaceOfSourceEndpoint, nameOfSourceEndpoint, nameOfEndpoint),
                DeadLetterExchangeName = GetDeadLetterExchangeName(namespaceOfEndpoint, nameOfSourceEndpoint, nameOfEndpoint)
            };
        }

        /// <summary>
        /// Creates settings for publisher's endpoint, with convention based exchange name
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="nameOfEndpoint">Endpoint name without "lykke" namespace</param>
        public static RabbitMqSubscriptionSettings ForPublisher(string connectionString, string nameOfEndpoint)
        {
            return ForPublisher(
                connectionString,
                LykkeNameSpace,
                nameOfEndpoint);
        }

        /// <summary>
        /// Creates settings for publisher's endpoint, with convention based exchange name
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="namespace">Endpoint's namespace</param>
        /// <param name="nameOfEndpoint">Endpoint's name</param>
        public static RabbitMqSubscriptionSettings ForPublisher(string connectionString, string @namespace, string nameOfEndpoint)
        {
            return new RabbitMqSubscriptionSettings
            {
                ConnectionString = connectionString,
                ExchangeName = GetExchangeNameWithValidation(@namespace, nameOfEndpoint)
            };
        }

        /// <summary>
        /// Creates settings for subscriber's endpoint, with convention based exchange, queue and dead letter queue names
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="nameOfSourceEndpoint">Endpoint's name, to which messages subscriber want to be subsribed, without "lykke" namespace</param>
        /// <param name="nameOfEndpoint">Subscribers's endpoint name, without "lykke" namepsace</param>
        public static RabbitMqSubscriptionSettings ForSubscriber(
            string connectionString,
            string nameOfSourceEndpoint,
            string nameOfEndpoint)
        {
            return ForSubscriber(
                connectionString,
                LykkeNameSpace,
                nameOfSourceEndpoint,
                LykkeNameSpace,
                nameOfEndpoint);
        }

        /// <summary>
        /// Creates settings for subscriber's endpoint, with convention based exchange, queue and dead letter queue names
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="namespaceOfSourceEndpoint">Endpoint's name, to which messages subscriber want to be subscribed</param>
        /// <param name="nameOfSourceEndpoint">Endpoint's name, to which messages subscriber want to be subsribed</param>
        /// <param name="namespaceOfEndpoint">Subscriber's endpoint namesace</param>
        /// <param name="nameOfEndpoint">Subscribers's endpoint name</param>
        public static RabbitMqSubscriptionSettings ForSubscriber(
            string connectionString,
            string namespaceOfSourceEndpoint,
            string nameOfSourceEndpoint,
            string namespaceOfEndpoint,
            string nameOfEndpoint)
        {
            return new RabbitMqSubscriptionSettings
            {
                ConnectionString = connectionString,
                ExchangeName = GetExchangeNameWithValidation(namespaceOfSourceEndpoint, nameOfSourceEndpoint),
                QueueName = GetQueueNameWithValidation(namespaceOfSourceEndpoint, nameOfSourceEndpoint, nameOfEndpoint),
                DeadLetterExchangeName = GetDeadLetterExchangeNameWithValidation(namespaceOfEndpoint, nameOfSourceEndpoint, nameOfEndpoint)
            };
        }

        public RabbitMqSubscriptionSettings MakeDurable()
        {
            IsDurable = true;

            return this;
        }

        public RabbitMqSubscriptionSettings UseRoutingKey(string routingKey)
        {
            RoutingKey = routingKey ?? string.Empty;

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

        private static string GetExchangeNameWithValidation(string @namespace, string endpointName)
        {
            if (@namespace == LykkeNameSpace && endpointName.StartsWith($"{LykkeNameSpace}."))
                return $"{NormalizeName(endpointName)}";
            return $"{NormalizeName(@namespace)}.{NormalizeName(endpointName)}";
        }

        private static string GetQueueNameWithValidation(string @namespace, string nameOfSourceEndpoint, string endpointName)
        {
            return $"{GetExchangeNameWithValidation(@namespace, nameOfSourceEndpoint)}.{NormalizeName(endpointName)}";
        }

        private static string GetDeadLetterExchangeNameWithValidation(string @namespace, string nameOfSourceEndpoint, string endpointName)
        {
            if (@namespace == LykkeNameSpace && endpointName.StartsWith($"{LykkeNameSpace}."))
                return $"{NormalizeName(endpointName)}.{NormalizeName(nameOfSourceEndpoint)}.dlx";
            return $"{NormalizeName(@namespace)}.{NormalizeName(endpointName)}.{NormalizeName(nameOfSourceEndpoint)}.dlx";
        }

        private static string NormalizeName(string appName)
        {
            return appName;
        }
    }
}
