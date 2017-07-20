using System;
using System.Collections.Generic;
using System.Text;
using Common.Log;
using Lykke.RabbitMqBroker.Publisher;
using Lykke.RabbitMqBroker.Subscriber;
using NSubstitute;
using NUnit.Framework;
using RabbitMQ.Client;

namespace RabbitMqBrokerTests
{
    [TestFixture]
    internal abstract class RabbitMqPublisherSubscriberBaseTest
    {
        protected IConnectionFactory _factory;
        protected RabbitMqSubscribtionSettings _settings;
        protected IConsole _console;
        protected ILog Log;
        protected const string ConnectionString = "amqp://guest:guest@localhost:5672";

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
        }

        [SetUp]
        public void SetUpBase()
        {
            _console = new ConsoleLWriter(Console.Write);
            Log = Substitute.For<ILog>();


            _settings = new RabbitMqSubscribtionSettings
            {
                ConnectionString = ConnectionString,
                DeadLetterExchangeName = DeadLetterExchangeName,
                ExchangeName = ExchangeName,
                IsDurable = true,
                QueueName = QueueName,
                RoutingKey = "RoutingKey"
            };

            _factory = new ConnectionFactory { Uri = ConnectionString };
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
            catch (Exception e)
            {
                _console.WriteLine("The rabbitmq server either not installed or not started");
                Assert.Inconclusive("The rabbitmq server either not installed or not started");
            }

        }

        protected  string ReadFromQueue(string queueName = QueueName, bool ack = true)
        {

            using (var connection = _factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                var consumer = new QueueingBasicConsumer(channel);
                channel.BasicConsume(queueName, false, consumer);
                consumer.Queue.Dequeue(1000, out var eventArgs);

                if (ack)
                {
                    channel.BasicAck(eventArgs.DeliveryTag, false);
                }
                else
                {
                    channel.BasicReject(eventArgs.DeliveryTag, false);
                }

                return Encoding.UTF8.GetString(eventArgs.Body);

            }
        }

        protected void SetupPoisonQueue()
        {
            using (var connection = _factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
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
                channel.QueueDeclare(QueueName, autoDelete: false, exclusive: false, durable: _settings.IsDurable, arguments: args);
                channel.QueueBind(QueueName, ExchangeName, _settings.RoutingKey);
            }
        }
    }
}
