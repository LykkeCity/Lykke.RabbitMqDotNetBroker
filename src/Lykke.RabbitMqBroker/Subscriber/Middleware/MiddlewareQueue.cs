using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client.Events;

namespace Lykke.RabbitMqBroker.Subscriber.Middleware
{
    internal class MiddlewareQueue<T> : IMiddlewareQueue<T>
    {
        private readonly List<IEventMiddleware<T>> _eventMiddlewares = new List<IEventMiddleware<T>>();
        private readonly RabbitMqSubscriptionSettings _settings;

        internal MiddlewareQueue(RabbitMqSubscriptionSettings settings)
        {
            _settings = settings;
        }

        public void AddMiddleware(IEventMiddleware<T> middleware)
        {
            _eventMiddlewares.Add(middleware);
        }

        public Task RunMiddlewaresAsync(
            BasicDeliverEventArgs basicDeliverEventArgs,
            T evt,
            IMessageAcceptor ma,
            CancellationToken cancellationToken)
        {
            var context = new EventContext<T>(
                basicDeliverEventArgs,
                evt,
                ma,
                _settings,
                0,
                this,
                cancellationToken);
            return _eventMiddlewares[0].ProcessAsync(context);
        }

        public IEventMiddleware<T> GetNext(int currentIndex)
        {
            if (currentIndex < 0)
                throw new IndexOutOfRangeException($"{nameof(currentIndex)} must be non-negative");

            if (currentIndex >= _eventMiddlewares.Count - 1)
                return null;

            return _eventMiddlewares[currentIndex + 1];
        }

        public bool HasMiddleware<TMiddleware>()
        {
            var type = typeof(TMiddleware);
            return _eventMiddlewares.Any(m => m.GetType() == type);
        }
    }
}
