// Copyright (c) Lykke Corp.
// Licensed under the MIT License. See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using Lykke.RabbitMqBroker.Subscriber.Strategies;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NUnit.Framework;

namespace Lykke.RabbitMqBroker.Tests
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
            _strategy = new DefaultErrorHandlingStrategy(new NullLogger<DefaultErrorHandlingStrategy>(), _settings);
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
            _strategy = new DefaultErrorHandlingStrategy(new NullLogger<DefaultErrorHandlingStrategy>(), _settings, nextHandler);

            var handler = new Action(() => throw new Exception());
            var acceptor = Substitute.For<IMessageAcceptor>();

            _strategy.Execute(handler, acceptor, CancellationToken.None);

            nextHandler.Received(1).Execute(handler, acceptor, CancellationToken.None);
        }
    }
}
