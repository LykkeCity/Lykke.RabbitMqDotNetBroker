using System;
using Common.Log;
using Lykke.RabbitMqBroker.Publisher;
using Lykke.RabbitMqBroker.Subscriber;
using Moq;
using NUnit.Framework;

namespace RabbitMqBrokerTests
{
    [TestFixture]
    internal sealed class PublisherConfigurationTests
    {
        private RabbitMqPublisher<string> _publisher;

        [SetUp]
        public void SetUp()
        {
            var settings = new RabbitMqSubscriptionSettings
            {
                ConnectionString = "",
                DeadLetterExchangeName = "",
                ExchangeName = "",
                IsDurable = true,
                QueueName = "",
                RoutingKey = "RoutingKey"
            };

            _publisher = new RabbitMqPublisher<string>(settings);

            _publisher
                .SetLogger(new Mock<ILog>().Object)
                .SetPublishStrategy(new DefaultFanoutPublishStrategy(settings))
                .SetSerializer(new JsonMessageSerializer<string>());
        }

        [Test]
        public void QueuePersistenceShouldBeConfiguredExplicitly()
        {
            Assert.Throws<InvalidOperationException>(() => _publisher.Start());

            _publisher.Stop();
        }

        [Test]
        public void QueueRepositoryCanBeSet()
        {
            _publisher.SetQueueRepository(new Mock<IPublishingQueueRepository<string>>().Object);

            Assert.DoesNotThrow(() => _publisher.Start());

            _publisher.Stop();
        }

        [Test]
        public void QueuePersistenceCanBeDisabled()
        {
            _publisher.DisableInMemoryQueuePersistence();

            Assert.DoesNotThrow(() => _publisher.Start());

            _publisher.Stop();
        }
    }
}