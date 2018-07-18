using System.Threading.Tasks;

namespace Lykke.RabbitMqBroker.Deduplication
{
    public interface IDeduplicator
    {
        Task<bool> EnsureNotDuplicateAsync(byte[] value);
    }
}
