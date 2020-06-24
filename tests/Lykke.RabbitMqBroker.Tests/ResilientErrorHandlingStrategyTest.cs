// Copyright (c) Lykke Corp.
// Licensed under the MIT License. See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using Lykke.RabbitMqBroker.Subscriber.ErrorHandlingStrategies;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NUnit.Framework;

namespace Lykke.RabbitMqBroker.Tests
{
    [TestFixture]
    internal class ResilientErrorHandlingStrategyTest
    {
        private ResilientErrorHandlingStrategy _strategy;

        [SetUp]
        public void SetUp()
        {
            var settings = new RabbitMqSubscriptionSettings
            {
                QueueName = "QueueName"
            };
            _strategy = new ResilientErrorHandlingStrategy(
                new NullLogger<ResilientErrorHandlingStrategy>(), settings, TimeSpan.FromMilliseconds(5));
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
