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
    public class DefaultErrorHandlingStrategyTest
    {
        private DefaultErrorHandlingStrategy _strategy;
        private RabbitMqSubscriptionSettings _settings;

        [SetUp]
        public void SetUp()
        {
            _settings = new RabbitMqSubscriptionSettings
            {
                QueueName = "QueueName"
            };
            _strategy = new DefaultErrorHandlingStrategy(EmptyLogFactory.Instance, _settings);
        }

        [Test]
        public void SuccessfulPath()
        {
            var handler = new Action(() => { });
            var acceptor = Substitute.For<IMessageAcceptor>();
            _strategy.Execute(handler, acceptor, CancellationToken.None);

            acceptor.Received(1).Accept();
        }

        [Test]
        public void ShouldResendToNextHandlerOnError()
        {
            var nextHandler = Substitute.For<IErrorHandlingStrategy>();
            _strategy = new DefaultErrorHandlingStrategy(EmptyLogFactory.Instance, _settings, nextHandler);

            var handler = new Action(() => throw new Exception());
            var acceptor = Substitute.For<IMessageAcceptor>();

            _strategy.Execute(handler, acceptor, CancellationToken.None);

            nextHandler.Received(1).Execute(handler, acceptor, CancellationToken.None);
        }
    }
}



