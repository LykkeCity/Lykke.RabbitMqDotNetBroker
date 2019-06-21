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
    [PublicAPI]
    public abstract class JsonRabbitSubscriber<TMessage> : IStartable, IStopable
    {
        private readonly ILogFactory _logFactory;
        private readonly string _connectionString;
        private readonly string _exchangeName;
        private readonly string _queueName;

        private RabbitMqSubscriber<TMessage> _subscriber;

        protected JsonRabbitSubscriber(
            string connectionString,
            string exchangeName,
            string queueName,
            ILogFactory logFactory)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _exchangeName = exchangeName ?? throw new ArgumentNullException(nameof(exchangeName));
            _queueName = queueName ?? throw new ArgumentNullException(nameof(queueName));
            _logFactory = logFactory ?? throw new ArgumentNullException(nameof(logFactory));
        }

        /// <inheritdoc cref="IStartable.Start"/>
        public void Start()
        {
            var rabbitMqSubscriptionSettings = RabbitMqSubscriptionSettings.ForSubscriber(
                    _connectionString,
                    _exchangeName,
                    _queueName)
                .MakeDurable();

            _subscriber = new RabbitMqSubscriber<TMessage>(
                    _logFactory,
                    rabbitMqSubscriptionSettings,
                    new ResilientErrorHandlingStrategy(
                        _logFactory,
                        rabbitMqSubscriptionSettings,
                        TimeSpan.FromSeconds(10)))
                .SetMessageDeserializer(new JsonMessageDeserializer<TMessage>())
                .Subscribe(ProcessMessageAsync)
                .CreateDefaultBinding()
                .Start();
        }

        /// <inheritdoc cref="IStopable.Stop"/>
        public void Stop()
        {
            _subscriber.Stop();
        }

        /// <inheritdoc cref="IDisposable"/>
        public void Dispose()
        {
            _subscriber.Dispose();
        }

        protected abstract Task ProcessMessageAsync(TMessage message);
    }
}
