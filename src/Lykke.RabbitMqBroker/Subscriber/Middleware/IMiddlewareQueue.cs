using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client.Events;

namespace Lykke.RabbitMqBroker.Subscriber.Middleware
{
    internal interface IMiddlewareQueue<T>
    {
        void AddMiddleware(IEventMiddleware<T> middleware);

        Task RunMiddlewaresAsync(
            BasicDeliverEventArgs basicDeliverEventArgs,
            T evt,
            IMessageAcceptor ma,
            CancellationToken cancellationToken);

        IEventMiddleware<T> GetNext(int currentIndex);

        bool HasMiddleware<TMiddleware>();
    }
}
