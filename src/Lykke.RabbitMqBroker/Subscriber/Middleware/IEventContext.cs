using System;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using RabbitMQ.Client;

namespace Lykke.RabbitMqBroker.Subscriber.Middleware
{
    public interface IEventContext<out T>
    {
        ReadOnlyMemory<byte> Body { get; }
        
        [CanBeNull] IBasicProperties BasicProperties { get; }

        T Event { get; }

        IMessageAcceptor MessageAcceptor { get; }

        RabbitMqSubscriptionSettings Settings { get; }

        CancellationToken CancellationToken { get; }

        Task InvokeNextAsync();
    }
}
