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
    public class DefaultErrorHandlingStrategyTest
    {
        private readonly RabbitMqSubscriptionSettings _settings = new RabbitMqSubscriptionSettings
        {
            QueueName = "QueueName"
        };
        private ExceptionSwallowMiddleware<string> _middleware;

        [SetUp]
        public void SetUp()
        {
            _middleware = new ExceptionSwallowMiddleware<string>(new NullLogger<ExceptionSwallowMiddleware<string>>());
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

        [Test]
        public void ShouldResendToNextHandlerOnError()
        {
            var acceptor = Substitute.For<IMessageAcceptor>();
            var rootEventMiddlewareHandler = Substitute.For<IEventMiddleware<string>>();
            var middlewarequeue = new MiddlewareQueue<string>(_settings);
            middlewarequeue.AddMiddleware(rootEventMiddlewareHandler);
            middlewarequeue.AddMiddleware(_middleware);
            middlewarequeue.AddMiddleware(new ActualHandlerMiddleware<string>(_ => throw new Exception()));

            middlewarequeue.RunMiddlewaresAsync(null, null, acceptor, CancellationToken.None).GetAwaiter().GetResult();

            rootEventMiddlewareHandler.Received(1).ProcessAsync(Arg.Any<IEventContext<string>>());
        }
    }
}
