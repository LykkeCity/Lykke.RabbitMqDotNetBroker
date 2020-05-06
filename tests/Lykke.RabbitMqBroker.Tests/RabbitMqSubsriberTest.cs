// Copyright (c) Lykke Corp.
// Licensed under the MIT License. See the LICENSE file in the project root for more information.

using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Lykke.Logs;
using Lykke.RabbitMqBroker;
using Lykke.RabbitMqBroker.Subscriber;
using NUnit.Framework;
using RabbitMQ.Client;

namespace RabbitMqBrokerTests
{
    [TestFixture(Category = "Integration")]
    [Explicit]
    internal sealed class RabbitMqSubsriberTest : RabbitMqPublisherSubscriberBaseTest
    {
        private RabbitMqSubscriber<string> _subscriber;

        [SetUp]

        public void SetUp()
        {
            _subscriber = new RabbitMqSubscriber<string>(
                    EmptyLogFactory.Instance, 
                    _settings, 
                    new DefaultErrorHandlingStrategy(EmptyLogFactory.Instance, _settings), 
                    submitTelemetry: true)
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
            _subscriber.Subscribe(handler);

            _subscriber.Start();

            PublishToQueue(expected);

            completeLock.Wait();
            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void ShouldUseDeadLetterQueueOnException()
        {
            _subscriber = new RabbitMqSubscriber<string>(
                    EmptyLogFactory.Instance,
                    _settings, 
                    new DeadQueueErrorHandlingStrategy(EmptyLogFactory.Instance, _settings))
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
            _subscriber.Subscribe(handler);
            _subscriber.Start();

            completeLock.Wait();
            
            var result = ReadFromQueue(PoisonQueueName);

            Assert.That(result, Is.EqualTo(expected));
        }
        
        [TearDown]
        public void TearDown()
        {
            ((IStopable)_subscriber).Stop();
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

