// Copyright (c) Lykke Corp.
// Licensed under the MIT License. See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Lykke.RabbitMqBroker.Subscriber.Middleware;
using Lykke.RabbitMqBroker.Subscriber.Middleware.ErrorHandling;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NUnit.Framework;

namespace Lykke.RabbitMqBroker.Tests
{
    [TestFixture]
    internal class ResilientErrorHandlingStrategyTest
    {
        private readonly RabbitMqSubscriptionSettings _settings = new RabbitMqSubscriptionSettings
        {
            QueueName = "QueueName"
        };
        private ResilientErrorHandlingMiddleware<string> _middleware;

        [SetUp]
        public void SetUp()
        {
            _middleware = new ResilientErrorHandlingMiddleware<string>(
                new NullLogger<ResilientErrorHandlingMiddleware<string>>(), TimeSpan.FromMilliseconds(5));
        }

        [Test]
        public void SuccessfulPath()
        {
            var acceptor = Substitute.For<IMessageAcceptor>();
            var middlewarequeue = new MiddlewareQueue<string>(_settings);
            middlewarequeue.AddMiddleware(_middleware);
            middlewarequeue.AddMiddleware(new ActualHandlerMiddleware<string>(_ => Task.CompletedTask));

            middlewarequeue.RunMiddlewaresAsync(null, null, acceptor, CancellationToken.None).GetAwaiter().GetResult();

            acceptor.Received(1).Accept();
        }
    }
}
