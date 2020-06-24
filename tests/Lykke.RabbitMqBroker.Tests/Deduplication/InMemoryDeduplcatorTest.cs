// Copyright (c) Lykke Corp.
// Licensed under the MIT License. See the LICENSE file in the project root for more information.

using System.Threading;
using Lykke.RabbitMqBroker.Subscriber.Middleware;
using Lykke.RabbitMqBroker.Subscriber.Middleware.Deduplication;
using NSubstitute;
using NUnit.Framework;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Framing;

namespace Lykke.RabbitMqBroker.Tests.Deduplication
{
    [TestFixture]
    public class InMemoryDeduplcatorTest
    {
        private readonly RabbitMqSubscriptionSettings _settings = new RabbitMqSubscriptionSettings
        {
            QueueName = "QueueName"
        };
        private InMemoryDeduplicationMiddleware<string> _deduplicationMiddleware;

        [SetUp]
        public void SetUp()
        {
            _deduplicationMiddleware = new InMemoryDeduplicationMiddleware<string>();
        }

        [Test]
        public void EnsureNotDuplicateAsync()
        {
            var value = new byte[] {1, 2, 3 };
            var acceptor = Substitute.For<IMessageAcceptor>();
            var lastMiddleware = Substitute.For<IEventMiddleware<string>>();
            var middlewarequeue = new MiddlewareQueue<string>(_settings);
            middlewarequeue.AddMiddleware(_deduplicationMiddleware);
            middlewarequeue.AddMiddleware(lastMiddleware);

            middlewarequeue.RunMiddlewaresAsync(
                new BasicDeliverEventArgs
                {
                    BasicProperties = new BasicProperties(),
                    Body = value
                },
                null,
                acceptor,
                CancellationToken.None)
                .GetAwaiter().GetResult();

            middlewarequeue.RunMiddlewaresAsync(
                    new BasicDeliverEventArgs
                    {
                        BasicProperties = new BasicProperties(),
                        Body = value
                    },
                    null,
                    acceptor,
                    CancellationToken.None)
                .GetAwaiter().GetResult();

            lastMiddleware.Received(1).ProcessAsync(Arg.Any<IEventContext<string>>());
        }
    }
}
