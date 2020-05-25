using System;
using System.Threading.Tasks;
using Autofac;
using JetBrains.Annotations;
using Lykke.RabbitMqBroker.Subscriber.Deserializers;
using Lykke.RabbitMqBroker.Subscriber.Strategies;
using Microsoft.Extensions.Logging;

namespace Lykke.RabbitMqBroker.Subscriber
{
    /// <summary>
    /// Base class for standard json-based subscriber
    /// </summary>
    /// <typeparam name="TMessage">Message type.</typeparam>
    [PublicAPI]
    public abstract class JsonRabbitSubscriber<TMessage> : IStartStop
    {
        private readonly RabbitMqSubscriptionSettings _settings;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ushort _prefetchCount;

        private RabbitMqSubscriber<TMessage> _subscriber;

        protected JsonRabbitSubscriber(
            RabbitMqSubscriptionSettings settings,
            ILoggerFactory logFactory)
            : this(
                settings,
                true,
                100,
                logFactory)
        {
        }

        protected JsonRabbitSubscriber(
            RabbitMqSubscriptionSettings settings,
            bool isDurable,
            ILoggerFactory logFactory)
            : this(
                settings,
                isDurable,
                100,
                logFactory)
        {
        }

        protected JsonRabbitSubscriber(
            RabbitMqSubscriptionSettings settings,
            bool isDurable,
            ushort prefetchCount,
            ILoggerFactory logFactory)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            if (isDurable)
                _settings = _settings.MakeDurable();
            _prefetchCount = prefetchCount;
            _loggerFactory = logFactory ?? throw new ArgumentNullException(nameof(logFactory));
        }

        /// <inheritdoc cref="IStartable.Start"/>
        public void Start()
        {
            _subscriber = new RabbitMqSubscriber<TMessage>(
                    _loggerFactory.CreateLogger<RabbitMqSubscriber<TMessage>>(),
                    _settings,
                    new ResilientErrorHandlingStrategy(
                        _loggerFactory.CreateLogger<ResilientErrorHandlingStrategy>(),
                        _settings,
                        TimeSpan.FromSeconds(10),
                        next: new DeadQueueErrorHandlingStrategy(
                            _loggerFactory.CreateLogger<DeadQueueErrorHandlingStrategy>(), _settings)))
                .SetMessageDeserializer(new JsonMessageDeserializer<TMessage>())
                .SetPrefetchCount(_prefetchCount)
                .Subscribe(ProcessMessageAsync)
                .CreateDefaultBinding()
                .Start();
        }

        /// <inheritdoc cref="IStartStop.Stop"/>
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
