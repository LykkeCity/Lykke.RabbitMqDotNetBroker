using System;
using System.Threading.Tasks;
using Autofac;
using Common;
using JetBrains.Annotations;
using Lykke.Common.Log;
using Lykke.RabbitMqBroker.Subscriber;

namespace Lykke.RabbitMqBroker.Publisher
{
    /// <summary>
    /// Standard implementation for IRabbitPublisher with json serializer.
    /// </summary>
    /// <typeparam name="TMessage"></typeparam>
    [PublicAPI]
    public class JsonRabbitPublisher<TMessage> : IRabbitPublisher<TMessage>
    {
        private readonly ILogFactory _logFactory;
        private readonly string _connectionString;
        private readonly string _exchangeName;
        private RabbitMqPublisher<TMessage> _rabbitMqPublisher;

        public JsonRabbitPublisher(
            ILogFactory logFactory,
            string connectionString,
            string exchangeName)
        {
            _logFactory = logFactory ?? throw new ArgumentNullException(nameof(logFactory));
            _connectionString = connectionString;
            _exchangeName = exchangeName;
        }

        /// <inheritdoc cref="IStartable.Start"/>
        public void Start()
        {
            var settings = RabbitMqSubscriptionSettings
                .ForPublisher(_connectionString, _exchangeName)
                .MakeDurable();

            _rabbitMqPublisher = new RabbitMqPublisher<TMessage>(_logFactory, settings)
                .SetSerializer(new JsonMessageSerializer<TMessage>())
                .SetPublishStrategy(new DefaultFanoutPublishStrategy(settings))
                .PublishSynchronously()
                .Start();
        }

        /// <inheritdoc cref="IStopable.Stop"/>
        public void Stop()
        {
            _rabbitMqPublisher?.Stop();
        }

        /// <inheritdoc cref="IDisposable"/>
        public void Dispose()
        {
            _rabbitMqPublisher?.Dispose();
        }

        /// <inheritdoc cref="IRabbitPublisher{TMessage}"/>
        public async Task PublishAsync(TMessage message)
        {
            await _rabbitMqPublisher.ProduceAsync(message);
        }
    }
}
