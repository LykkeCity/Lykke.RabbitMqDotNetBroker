using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lykke.RabbitMqBroker.Publisher
{
    public interface IPublishingQueueRepository
    {
        Task SaveAsync(IReadOnlyCollection<RawMessage> items, string exchangeName);

        Task<IReadOnlyCollection<RawMessage>> LoadAsync(string exchangeName);
    }
}
