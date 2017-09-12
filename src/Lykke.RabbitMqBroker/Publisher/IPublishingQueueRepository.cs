using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lykke.RabbitMqBroker.Publisher
{
    public interface IPublishingQueueRepository<TMessageModel>
    {
        Task SaveAsync(IReadOnlyCollection<TMessageModel> items, string exchangeName);
        Task<IReadOnlyCollection<TMessageModel>> LoadAsync(string exchangeName);
    } 
}