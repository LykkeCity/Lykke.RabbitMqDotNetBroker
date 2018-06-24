using System.Threading.Tasks;
using Common;
using Lykke.Logs;
using Lykke.RabbitMqBroker.Publisher;
using NUnit.Framework;

namespace RabbitMqBrokerTests
{
    using System;
    using Lykke.RabbitMqBroker;
    using Lykke.RabbitMqBroker.Subscriber;

    using Newtonsoft.Json;

    using NSubstitute;
    using RabbitMQ.Client;

    [TestFixture(Category = "Integration"), Explicit]
    public class SyncPublisherTest : RabbitMqPublisherSubscriberBaseTest
    {
        private RabbitMqPublisher<string> _publisher;

        [SetUp]
        public void SetUp()
        {
            _publisher = new RabbitMqPublisher<string>(EmptyLogFactory.Instance, _settings);

            _publisher
                .SetConsole(_console)
                .SetPublishStrategy(new DefaultFanoutPublishStrategy(_settings))
                .DisableInMemoryQueuePersistence()
                .SetSerializer(new TestMessageSerializer());
        }

        [TearDown]
        public void TearDown()
        {
            ((IStopable)_publisher).Stop();
        }


        [Test]
        public async Task SuccessfulPath()
        {
            _publisher.PublishSynchronously();
            _publisher.Start();

            SetupNormalQueue();
            const string Expected = "expected";

            await _publisher.ProduceAsync(Expected);

            var result = ReadFromQueue();
            Assert.That(result, Is.EqualTo(Expected));
        }

        [Test]
        public void ShouldNotPublishNonSeriazableMessage()
        {
            var publisher = new RabbitMqPublisher<ComplexType>(EmptyLogFactory.Instance, _settings);

            publisher
                .SetConsole(_console)
                .SetPublishStrategy(new DefaultFanoutPublishStrategy(_settings))
                .DisableInMemoryQueuePersistence()
                .SetSerializer(new JsonMessageSerializer<ComplexType>())
                .Start();

            var invalidObj = new ComplexType
            {
                A = 10
            };
            invalidObj.B = invalidObj;

            Assert.ThrowsAsync<JsonSerializationException>(() => publisher.ProduceAsync(invalidObj));
        }


        [Test]
        public void ShouldRethrowPublishingException()
        {
            var bu = new TestBuffer();
            bu.Gate.Set();
            SetupNormalQueue();


            var pubStrategy = Substitute.For<IRabbitMqPublishStrategy>();
            _publisher.SetBuffer(bu);
            _publisher.SetPublishStrategy(pubStrategy);
            _publisher.PublishSynchronously();
            _publisher.Start();

            pubStrategy.When(m => m.Publish(Arg.Any<RabbitMqSubscriptionSettings>(), Arg.Any<IModel>(), Arg.Any<RawMessage>())).Throw<InvalidOperationException>();


            Assert.Throws<RabbitMqBrokerException>(() => _publisher.ProduceAsync(string.Empty).Wait());
        }

        private class ComplexType
        {
            public int A;

            public ComplexType B;
        }
    }
}
