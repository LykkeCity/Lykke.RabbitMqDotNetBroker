using System;
using System.Threading.Tasks;
using Autofac;
using Common;
using JetBrains.Annotations;
using Lykke.Common.Log;

namespace Lykke.RabbitMqBroker.Subscriber
{
    /// <summary>
    /// Base class for standard json-based subscriber
    /// </summary>
    /// <typeparam name="TMessage">Message type.</typeparam>
    [PublicAPI]
    public abstract class JsonRabbitSubscriber<TMessage> : IStartable, IStopable
    {
        private readonly string _connectionString;
        private readonly string _exchangeName;
        private readonly string _queueName;
        private readonly bool _isDurable;
        private readonly ILogFactory _logFactory;
        private readonly ushort _prefetchCount;

        private RabbitMqSubscriber<TMessage> _subscriber;

        protected JsonRabbitSubscriber(
            string connectionString,
            string exchangeName,
            string queueName,
            ILogFactory logFactory)
            : this(
                connectionString,
                exchangeName,
                queueName,
                true,
                100,
                logFactory)
        {
        }

        protected JsonRabbitSubscriber(
            string connectionString,
            string exchangeName,
            string queueName,
            bool isDurable,
            ILogFactory logFactory)
            : this(
                connectionString,
                exchangeName,
                queueName,
                isDurable,
                100,
                logFactory)
        {
        }

        protected JsonRabbitSubscriber(
            string connectionString,
            string exchangeName,
            string queueName,
            bool isDurable,
            ushort prefetchCount,
            ILogFactory logFactory)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _exchangeName = exchangeName ?? throw new ArgumentNullException(nameof(exchangeName));
            _queueName = queueName ?? throw new ArgumentNullException(nameof(queueName));
            _isDurable = isDurable;
            _prefetchCount = prefetchCount;
            _logFactory = logFactory ?? throw new ArgumentNullException(nameof(logFactory));
        }

        /// <inheritdoc cref="IStartable.Start"/>
        public void Start()
        {
            var rabbitMqSubscriptionSettings = RabbitMqSubscriptionSettings.ForSubscriber(
                _connectionString,
                _exchangeName,
                _queueName);
            if (_isDurable)
                rabbitMqSubscriptionSettings = rabbitMqSubscriptionSettings.MakeDurable();

            _subscriber = new RabbitMqSubscriber<TMessage>(
                    _logFactory,
                    rabbitMqSubscriptionSettings,
                    new ResilientErrorHandlingStrategy(
                        _logFactory,
                        rabbitMqSubscriptionSettings,
                        TimeSpan.FromSeconds(10)))
                .SetMessageDeserializer(new JsonMessageDeserializer<TMessage>())
                .SetPrefetchCount(_prefetchCount)
                .Subscribe(ProcessMessageAsync)
                .CreateDefaultBinding()
                .Start();
        }

        /// <inheritdoc cref="IStopable.Stop"/>
        public void Stop()
        {
            if (_subscriber != null)
            {
                _subscriber.Stop();
                _subscriber.Dispose();
                _subscriber = null;
            }
        }

        /// <inheritdoc cref="IDisposable"/>
        public void Dispose()
        {
            Stop();
        }

        protected abstract Task ProcessMessageAsync(TMessage message);
    }
}
