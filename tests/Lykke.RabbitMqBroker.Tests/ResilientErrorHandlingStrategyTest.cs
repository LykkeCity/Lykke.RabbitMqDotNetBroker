using System;
using System.Threading;
using Common.Log;
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
            ILog log = Substitute.For<ILog>();
            RabbitMqSubscriptionSettings settings = new RabbitMqSubscriptionSettings
            {
                QueueName = "QueueName"
            };
            _strategy = new ResilientErrorHandlingStrategy(log, settings, TimeSpan.FromMilliseconds(5));
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
