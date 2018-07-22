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
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1);

        public AzureStorageDeduplicator(INoSQLTableStorage<DuplicateEntity> tableStorage)
        {
            _repository = new DuplicatesRepository(tableStorage);
        }
        
        public async Task<bool> EnsureNotDuplicateAsync(byte[] value)
        {
            await _lock.WaitAsync();
            
            try
            {
                if (await _repository.DuplicateExistsAsync(value))
                    return false;
                
                await _repository.AddAsync(value);
            }
            finally
            {
                _lock.Release();
            }

            return true;
        }
    }
}
