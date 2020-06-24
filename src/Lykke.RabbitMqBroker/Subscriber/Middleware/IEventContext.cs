using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client.Events;

namespace Lykke.RabbitMqBroker.Subscriber.Middleware
{
    public interface IEventContext<out T>
    {
        BasicDeliverEventArgs BasicDeliverEventArgs { get; }

        T Event { get; }

        IMessageAcceptor MessageAcceptor { get; }

        RabbitMqSubscriptionSettings Settings { get; }

        CancellationToken CancellationToken { get; }

        Task InvokeNextAsync();
    }
}
