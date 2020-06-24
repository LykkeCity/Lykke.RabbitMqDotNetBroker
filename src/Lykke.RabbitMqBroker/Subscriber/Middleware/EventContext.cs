using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client.Events;

namespace Lykke.RabbitMqBroker.Subscriber.Middleware
{
    internal class EventContext<T> : IEventContext<T>
    {
        private readonly int _middlewareQueueIndex;
        private readonly IMiddlewareQueue<T> _middlewareQueue;

        public BasicDeliverEventArgs BasicDeliverEventArgs { get; }
        public T Event { get; }
        public IMessageAcceptor MessageAcceptor { get; }
        public RabbitMqSubscriptionSettings Settings { get; }
        public CancellationToken CancellationToken { get; }

        internal EventContext(
            BasicDeliverEventArgs basicDeliverEventArgs,
            T evt,
            IMessageAcceptor ma,
            RabbitMqSubscriptionSettings settings,
            int middlewareQueueIndex,
            IMiddlewareQueue<T> middlewareQueue,
            CancellationToken cancellationToken)
        {
            BasicDeliverEventArgs = basicDeliverEventArgs;
            Event = evt;
            MessageAcceptor = ma;
            Settings = settings;
            CancellationToken = cancellationToken;

            _middlewareQueueIndex = middlewareQueueIndex;
            _middlewareQueue = middlewareQueue;
        }

        public Task InvokeNextAsync()
        {
            var next = _middlewareQueue.GetNext(_middlewareQueueIndex);
            var contextForNext = new EventContext<T>(
                BasicDeliverEventArgs,
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
