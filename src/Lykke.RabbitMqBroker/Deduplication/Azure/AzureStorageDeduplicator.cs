using System.Threading;
using System.Threading.Tasks;
using AzureStorage;

namespace Lykke.RabbitMqBroker.Deduplication.Azure
{
    /// <inheritdoc />
    /// <remarks>Azure table storage implementation</remarks>
    public class AzureStorageDeduplicator : IDeduplicator
    {
        private DuplicatesRepository _repository;

        public AzureStorageDeduplicator(INoSQLTableStorage<DuplicateEntity> tableStorage)
        {
            _repository = new DuplicatesRepository(tableStorage);
        }
        
        public async Task<bool> EnsureNotDuplicateAsync(byte[] value)
        {
            return await _repository.DuplicateExistsAsync(value);
        }
    }
}
