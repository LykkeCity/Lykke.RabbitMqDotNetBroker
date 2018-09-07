using System.Threading;
using System.Threading.Tasks;
using Common;
using Lykke.Logs;
using Lykke.RabbitMqBroker.Publisher;
using NUnit.Framework;

namespace RabbitMqBrokerTests
{
    [TestFixture(Category = "Integration"), Explicit]
    internal sealed class AsyncPublisherTest : RabbitMqPublisherSubscriberBaseTest
    {
        private RabbitMqPublisher<string> _publisher;

        [SetUp]
        public void SetUp()
        {
            _publisher = new RabbitMqPublisher<string>(EmptyLogFactory.Instance, _settings);

            _publisher
                .SetPublishStrategy(new DefaultFanoutPublishStrategy(_settings))
                .DisableInMemoryQueuePersistence()
                .SetSerializer(new TestMessageSerializer());
        }

        [Test]
        public async Task SimplePublish()
        {
            const string expected = "GetDefaultHost message";

            _publisher.Start();

            SetupNormalQueue();

            await _publisher.ProduceAsync(expected);

            var result = ReadFromQueue();

            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public async Task PublishingOneByOne()
        {
            _publisher.Start();

            SetupNormalQueue();

            for (int i = 0; i < 10; i++)
            {
                var expected = i.ToString();
                await _publisher.ProduceAsync(expected);
                var result = ReadFromQueue();
                Assert.That(result, Is.EqualTo(expected));
            }
        }

        [Test]
        public async Task ShouldUseBuffer()
        {
            var bu = new TestBuffer();
            _publisher.SetBuffer(bu);
            _publisher.Start();

            SetupNormalQueue();

            const int msgCount = 10;
            for (int i = 0; i < msgCount; i++)
            {
                var expected = i.ToString();
                await _publisher.ProduceAsync(expected);
                var result = ReadFromQueue();
                Assert.That(result, Is.EqualTo(string.Empty));
            }

            Assert.That(bu.Count, Is.EqualTo(msgCount));
            bu.Gate.Set();
            while (bu.Count > 0)
            {
                Thread.Sleep(1);
            }

            for (int i = 0; i < msgCount; i++)
            {
                var expected = i.ToString();
                var result = ReadFromQueue();
                Assert.That(result, Is.EqualTo(expected));
            }
        }

        [Test]
        public async Task ShouldUseDeadLetterExchange()
        {
            const string expected = "GetDefaultHost message";


            _publisher.SetPublishStrategy(new DefaultFanoutPublishStrategy(_settings));

            _publisher.Start();

            SetupNormalQueue();
            SetupPoisonQueue();

            await _publisher.ProduceAsync(expected);


            // Reject the message
            ReadFromQueue(QueueName, false);

            var result = ReadFromQueue(PoisonQueueName);

            Assert.That(result, Is.EqualTo(expected));
        }

        [TearDown]
        public void TearDown()
        {
            ((IStopable)_publisher).Stop();
        }
    }
}

