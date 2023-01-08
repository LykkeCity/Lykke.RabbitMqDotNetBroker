// Copyright (c) Lykke Corp.
// Licensed under the MIT License. See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using JetBrains.Annotations;
using Lykke.RabbitMqBroker.Logging;
using Lykke.RabbitMqBroker.Publisher;
using Lykke.RabbitMqBroker.Publisher.Serializers;
using NUnit.Framework;
using RabbitMQ.Client;

namespace Lykke.RabbitMqBroker.Tests
{
    [TestFixture]
    public abstract class RabbitMqPublisherSubscriberBaseTest
    {
        protected IConnectionFactory _factory;
        protected RabbitMqSubscriptionSettings _settings;
        protected const string RabbitConnectionString = "amqp://[username]:[password]@[URL]:5672/IntegrationTests";

        protected const string ExchangeName = "TestExchange";
        protected const string QueueName = "TestQueue";
        protected const string DeadLetterExchangeName = ExchangeName + "-DL";
        protected const string PoisonQueueName = QueueName + "-poison";

        protected class TestMessageSerializer : IRabbitMqSerializer<string>
        {
            public byte[] Serialize(string model)
            {
                return Encoding.UTF8.GetBytes(model);
            }

            public SerializationFormat SerializationFormat { get; } = SerializationFormat.Unknown;
        }

        [SetUp]
        public void SetUpBase()
        {
            _settings = new RabbitMqSubscriptionSettings
            {
                ConnectionString = RabbitConnectionString,
                DeadLetterExchangeName = DeadLetterExchangeName,
                ExchangeName = ExchangeName,
                IsDurable = true,
                QueueName = QueueName,
                RoutingKey = "RoutingKey"
            };

            _factory = new ConnectionFactory {Uri = new Uri(RabbitConnectionString, UriKind.Absolute)};

            EnsureRabbitInstalledAndRun();
        }

        private void EnsureRabbitInstalledAndRun()
        {
            try
            {
                using (var connection = _factory.CreateConnection())
                {
                }
            }
            catch (Exception)
            {
                Assert.Inconclusive("The rabbitmq server either not installed or not started");
            }
        }

        protected string ReadFromQueue(string queueName = QueueName, bool ack = true)
        {

            using (var connection = _factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                var result = channel.BasicGet(queueName, ack);
                if (result != null)
                {
                    if (ack)
                    {
                        channel.BasicAck(result.DeliveryTag, false);
                    }
                    else
                    {
                        channel.BasicReject(result.DeliveryTag, false);
                    }

                    return Encoding.UTF8.GetString(result.Body.ToArray());
                }
                return string.Empty;
            }
        }

        protected void SetupPoisonQueue()
        {
            using (var connection = _factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.ExchangeDeclare(DeadLetterExchangeName, "direct", durable: true, autoDelete: false);
                channel.QueueDeclare(PoisonQueueName, autoDelete: false, durable: true, exclusive: false);
                channel.QueueBind(PoisonQueueName, DeadLetterExchangeName, _settings.RoutingKey);
            }
        }

        protected void SetupNormalQueue()
        {
            using (var connection = _factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                var args = new Dictionary<string, object>
                {
                    {"x-dead-letter-exchange", DeadLetterExchangeName},
                };
                channel.ExchangeDeclare(ExchangeName, "fanout", true, false);
                channel.QueueDeclare(QueueName, autoDelete: false, exclusive: false, durable: _settings.IsDurable, arguments: args);
                channel.QueueBind(QueueName, ExchangeName, _settings.RoutingKey);
            }
        }

        protected class TestBuffer : IPublisherBuffer
        {
            public readonly ManualResetEventSlim Gate = new ManualResetEventSlim(false);
            private readonly InMemoryBuffer _buffer = new InMemoryBuffer();

            public IEnumerator<RawMessage> GetEnumerator()
            {
                return _buffer.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public void Dispose()
            {
                _buffer.Dispose();
            }

            public int Count => _buffer.Count;

            public void Enqueue(RawMessage message, CancellationToken cancellationToken)
            {
                _buffer.Enqueue(message, cancellationToken);
            }

            public void Dequeue(CancellationToken cancellationToken)
            {
                _buffer.Dequeue(cancellationToken);
            }
            
            [CanBeNull]
            public RawMessage WaitOneAndPeek(CancellationToken cancellationToken)
            {
                Gate.Wait( cancellationToken);
                return _buffer.WaitOneAndPeek( cancellationToken);
            }
        }
    }
}
