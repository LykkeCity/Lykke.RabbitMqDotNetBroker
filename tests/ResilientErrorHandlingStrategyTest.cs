using System;
using System.Collections.Generic;
using System.Text;
using Common.Log;
using Lykke.RabbitMqBroker;
using Lykke.RabbitMqBroker.Subscriber;
using NSubstitute;
using NUnit.Framework.Internal;
using NUnit.Framework;

namespace RabbitMqBrokerTests
{
    [TestFixture]
    internal class ResilientErrorHandlingStrategyTest
    {
        private ResilientErrorHandlingStrategy _strategy;
        private ILog _log;
        private RabbitMqSubscriptionSettings _settings;

        [SetUp]
        public void SetUp()
        {
            _log = Substitute.For<ILog>();
            _settings = new RabbitMqSubscriptionSettings
            {
                QueueName = "QueueName"
            };
            _strategy = new ResilientErrorHandlingStrategy(_log, _settings, TimeSpan.FromMilliseconds(5));
        }

        [Test]
        public void SuccessfulPath()
        {
            var handler = new Action(() => { });
            var acceptor = Substitute.For<IMessageAcceptor>();
            _strategy.Execute(handler, acceptor);

            acceptor.Received(1).Accept();
        }
    }
}
