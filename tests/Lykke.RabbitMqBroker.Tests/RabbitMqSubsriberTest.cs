// Copyright (c) Lykke Corp.
// Licensed under the MIT License. See the LICENSE file in the project root for more information.

using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Lykke.RabbitMqBroker.Subscriber;
using Lykke.RabbitMqBroker.Subscriber.Deserializers;
using Lykke.RabbitMqBroker.Subscriber.Middleware.ErrorHandling;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using RabbitMQ.Client;

namespace Lykke.RabbitMqBroker.Tests
{
    [TestFixture(Category = "Integration")]
    [Explicit]
    internal sealed class RabbitMqSubsriberTest : RabbitMqPublisherSubscriberBaseTest
    {
        private RabbitMqPullingSubscriber<string> _pullingSubscriber;

        [SetUp]
        public void SetUp()
        {
            _pullingSubscriber = new RabbitMqPullingSubscriber<string>(
                    new NullLogger<RabbitMqPullingSubscriber<string>>(),
                    _settings)
                .UseMiddleware(new ExceptionSwallowMiddleware<string>(new NullLogger<ExceptionSwallowMiddleware<string>>()))
                .CreateDefaultBinding()
                .SetMessageDeserializer(new DefaultStringDeserializer());
        }

        [Test]
        public void SuccessfulPath()
        {
            const string expected = "GetDefaultHost message";

            string result = null;
            SetupNormalQueue();
            var completeLock = new ManualResetEventSlim(false);
            var handler = new Func<string, Task>(s =>
            {
                result = s;
                completeLock.Set();
                return Task.CompletedTask;
            });
            _pullingSubscriber.Subscribe(handler);

            _pullingSubscriber.Start();

            PublishToQueue(expected);

            completeLock.Wait();
            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void ShouldUseDeadLetterQueueOnException()
        {
            _pullingSubscriber = new RabbitMqPullingSubscriber<string>(
                    new NullLogger<RabbitMqPullingSubscriber<string>>(),
                    _settings)
                .UseMiddleware(new ExceptionSwallowMiddleware<string>(new NullLogger<ExceptionSwallowMiddleware<string>>()))
                .CreateDefaultBinding()
                .SetMessageDeserializer(new DefaultStringDeserializer());

            const string expected = "GetDefaultHost message";

            SetupNormalQueue();
            PublishToQueue(expected);

            var completeLock = new ManualResetEventSlim(false);
            var handler = new Func<string, Task>(s =>
            {
                completeLock.Set();
                throw new Exception();
            });
            _pullingSubscriber.Subscribe(handler);
            _pullingSubscriber.Start();

            completeLock.Wait();

            var result = ReadFromQueue(PoisonQueueName);

            Assert.That(result, Is.EqualTo(expected));
        }

        [TearDown]
        public void TearDown()
        {
            _pullingSubscriber.Stop();
        }

        private void PublishToQueue(string message)
        {
            var factory = new ConnectionFactory {Uri = new Uri(RabbitConnectionString, UriKind.Absolute)};

            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                var body = Encoding.UTF8.GetBytes(message);
                channel.BasicPublish(_settings.ExchangeName, _settings.RoutingKey, body: body);
            }
        }
    }
}
