using System.Threading.Tasks;

namespace Lykke.RabbitMqBroker.Publisher
{
    public interface IMessageProducer<in T>
    {
        Task ProduceAsync(T message);
    }
}
