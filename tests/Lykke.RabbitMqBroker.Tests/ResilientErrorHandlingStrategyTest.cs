using System;
using System.Threading;
using Lykke.Logs;
using Lykke.RabbitMqBroker;
using Lykke.RabbitMqBroker.Subscriber;
using NSubstitute;
using NUnit.Framework;

namespace RabbitMqBrokerTests
{
    [TestFixture]
    internal class ResilientErrorHandlingStrategyTest
    {
        private ResilientErrorHandlingStrategy _strategy;

        [SetUp]
        public void SetUp()
        {
            RabbitMqSubscriptionSettings settings = new RabbitMqSubscriptionSettings
            {
                QueueName = "QueueName"
            };
            _strategy = new ResilientErrorHandlingStrategy(EmptyLogFactory.Instance, settings, TimeSpan.FromMilliseconds(5));
        }

        [Test]
        public void SuccessfulPath()
        {
            var handler = new Action(() => { });
            var acceptor = Substitute.For<IMessageAcceptor>();
            _strategy.Execute(handler, acceptor, CancellationToken.None);

            acceptor.Received(1).Accept();
        }
    }
}
