using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lykke.RabbitMqBroker.Publisher
{
    public interface IPublishingQueueRepository
    {
        Task SaveAsync(IReadOnlyCollection<byte[]> items, string exchangeName);

        Task<IReadOnlyCollection<byte[]>> LoadAsync(string exchangeName);
    }
}