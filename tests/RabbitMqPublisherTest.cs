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
                .SetSerializer(new TestMessageSerializer());

        }

        [Test]
        public void SimplePublish()
        {
            const string expected = "Test message";

            _publisher.Start();

            SetupNormalQueue();

            _publisher.ProduceAsync(expected).Wait();

            var result = ReadFromQueue();


            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void ShouldUseDeadLetterExchange()
        {
            const string expected = "Test message";


            _publisher.SetPublishStrategy(new DefaultFanoutPublishStrategy(_settings));

            _publisher.Start();

            SetupNormalQueue();
            SetupPoisonQueue();

            _publisher.ProduceAsync(expected).Wait();


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

