using System;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using RabbitMQ.Client;

namespace Lykke.RabbitMqBroker.Subscriber.Middleware
{
    internal interface IMiddlewareQueue<T>
    {
        void AddMiddleware(IEventMiddleware<T> middleware);

        Task RunMiddlewaresAsync(
            ReadOnlyMemory<byte> body,
            [CanBeNull] IBasicProperties properties,
            T evt,
            IMessageAcceptor ma,
            CancellationToken cancellationToken);

        IEventMiddleware<T> GetNext(int currentIndex);

        bool HasMiddleware<TMiddleware>();
    }
}
