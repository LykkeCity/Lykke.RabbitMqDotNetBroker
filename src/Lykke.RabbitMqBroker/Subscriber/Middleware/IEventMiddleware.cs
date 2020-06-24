using System.Threading.Tasks;

namespace Lykke.RabbitMqBroker.Subscriber.Middleware
{
    public interface IEventMiddleware<T>
    {
        Task ProcessAsync(IEventContext<T> context);
    }
}
