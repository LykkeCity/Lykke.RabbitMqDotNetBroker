using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Lykke.Common.Log;
using Lykke.RabbitMqBroker.Subscriber;

namespace Lykke.RabbitMqBroker.Publisher
{
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

        public void Stop()
        {
            _rabbitMqPublisher?.Stop();
        }

        public void Dispose()
        {
            _rabbitMqPublisher?.Dispose();
        }

        public async Task PublishAsync(TMessage message)
        {
            await _rabbitMqPublisher.ProduceAsync(message);
        }
    }
}
