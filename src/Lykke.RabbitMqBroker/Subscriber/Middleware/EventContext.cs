using System;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using RabbitMQ.Client;

namespace Lykke.RabbitMqBroker.Subscriber.Middleware
{
    internal class EventContext<T> : IEventContext<T>
    {
        private readonly int _middlewareQueueIndex;
        private readonly IMiddlewareQueue<T> _middlewareQueue;

        public ReadOnlyMemory<byte> Body { get; }
        [CanBeNull] public IBasicProperties BasicProperties { get; }
        public T Event { get; }
        public IMessageAcceptor MessageAcceptor { get; }
        public RabbitMqSubscriptionSettings Settings { get; }
        public CancellationToken CancellationToken { get; }

        internal EventContext(
            ReadOnlyMemory<byte> body,
            [CanBeNull] IBasicProperties properties,
            T evt,
            IMessageAcceptor ma,
            RabbitMqSubscriptionSettings settings,
            int middlewareQueueIndex,
            IMiddlewareQueue<T> middlewareQueue,
            CancellationToken cancellationToken)
        {
            Event = evt;
            MessageAcceptor = ma;
            Settings = settings;
            CancellationToken = cancellationToken;
            Body = body;
            BasicProperties = properties;
            _middlewareQueueIndex = middlewareQueueIndex;
            _middlewareQueue = middlewareQueue;
        }

        public Task InvokeNextAsync()
        {
            var next = _middlewareQueue.GetNext(_middlewareQueueIndex);
            var contextForNext = new EventContext<T>(
                Body,
                BasicProperties,
                Event,
                MessageAcceptor,
                Settings,
                _middlewareQueueIndex + 1,
                _middlewareQueue,
                CancellationToken);
            return next.ProcessAsync(contextForNext);
        }
    }
}
