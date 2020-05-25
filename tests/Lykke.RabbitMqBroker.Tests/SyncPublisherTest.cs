// Copyright (c) Lykke Corp.
// Licensed under the MIT License. See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Lykke.RabbitMqBroker.Publisher;
using Lykke.RabbitMqBroker.Publisher.Serializers;
using Lykke.RabbitMqBroker.Publisher.Strategies;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using NSubstitute;
using NUnit.Framework;
using RabbitMQ.Client;

namespace Lykke.RabbitMqBroker.Tests
{
    [TestFixture(Category = "Integration"), Explicit]
    public class SyncPublisherTest : RabbitMqPublisherSubscriberBaseTest
    {
        private RabbitMqPublisher<string> _publisher;

        [SetUp]
        public void SetUp()
        {
            _publisher = new RabbitMqPublisher<string>(new NullLoggerFactory(), _settings);

            _publisher
                .SetPublishStrategy(new DefaultFanoutPublishStrategy(_settings))
                .DisableInMemoryQueuePersistence()
                .SetSerializer(new TestMessageSerializer());
        }

        [TearDown]
        public void TearDown()
        {
            _publisher.Stop();
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
            var publisher = new RabbitMqPublisher<ComplexType>(new NullLoggerFactory(), _settings);

            publisher
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
