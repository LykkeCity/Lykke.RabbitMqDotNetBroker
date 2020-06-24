using System;
using System.Threading;
using System.Threading.Tasks;

namespace Lykke.RabbitMqBroker.Subscriber.Middleware
{
    internal class ActualHandlerMiddleware<T> : IEventMiddleware<T>
    {
        private readonly Func<T, Task> _eventHandler;
        private readonly Func<T, CancellationToken, Task> _cancellableEventHandler;

        internal ActualHandlerMiddleware(Func<T, Task> eventHandler)
        {
            _eventHandler = eventHandler ?? throw new ArgumentNullException();
        }

        internal ActualHandlerMiddleware(Func<T, CancellationToken, Task> cancellableEventHandler)
        {
            _cancellableEventHandler = cancellableEventHandler ?? throw new ArgumentNullException();
        }

        public Task ProcessAsync(IEventContext<T> context)
        {
            return _cancellableEventHandler != null
                ? _cancellableEventHandler(context.Event, context.CancellationToken)
                : _eventHandler(context.Event);
        }
    }
}
