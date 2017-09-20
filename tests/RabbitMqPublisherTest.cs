using System.Threading.Tasks;
using Common;
using Lykke.RabbitMqBroker.Publisher;
using NUnit.Framework;

namespace RabbitMqBrokerTests
{
    [TestFixture(Category = "Integration")]
    internal sealed class RabbitMqPublisherTest : RabbitMqPublisherSubscriberBaseTest
    {
        private RabbitMqPublisher<string> _publisher;

        [SetUp]
        public void SetUp()
        {

            _publisher = new RabbitMqPublisher<string>(_settings);

            _publisher
                .SetConsole(_console)
                .SetPublishStrategy(new DefaultFanoutPublishStrategy(_settings))
                .DisableInMemoryQueuePersistence()
                .SetSerializer(new TestMessageSerializer())
                .SetLogger(Log);

        }

        [Test]
        public async Task SimplePublish()
        {
            const string expected = "Test message";

            _publisher.Start();

            SetupNormalQueue();

            await _publisher.ProduceAsync(expected);

            var result = ReadFromQueue();


            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public async Task ShouldUseDeadLetterExchange()
        {
            const string expected = "Test message";


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

