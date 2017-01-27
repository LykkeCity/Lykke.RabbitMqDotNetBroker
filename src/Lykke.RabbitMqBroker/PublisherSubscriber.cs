using System;
using System.Threading.Tasks;

namespace Lykke.PublisherSubscriber
{

    public interface IMessageProducer<T>
    {
        Task ProduceAsync(T message);
    }

    public interface IMessageConsumer<T>
    {
        void Subscribe(Func<T, Task> callback);
    }

}