using System;
using Common.Log;
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
        private ILog _log;
        private RabbitMqSubscribtionSettings _settings;

        [SetUp]
        public void SetUp()
        {
            _log = Substitute.For<ILog>();
            _settings = new RabbitMqSubscribtionSettings
            {
                QueueName = "QueueName"
            };
            _strategy = new DefaultErrorHandlingStrategy(_log, _settings);
        }

        [Test]
        public void SuccessfulPath()
        {
            var handler = new Action(() => { });
            var acceptor = Substitute.For<IMessageAcceptor>();
            _strategy.Execute(handler, acceptor);

            acceptor.Received(1).Accept();
        }

        [Test]
        public void ShouldResendToNextHandlerOnError()
        {
            var nextHandler = Substitute.For<IErrorHandlingStrategy>();
            _strategy = new DefaultErrorHandlingStrategy(_log, _settings, nextHandler);

            var handler = new Action(() => throw new Exception());
            var acceptor = Substitute.For<IMessageAcceptor>();

            _strategy.Execute(handler, acceptor);

            nextHandler.Received(1).Execute(handler, acceptor);
        }
    }
}



