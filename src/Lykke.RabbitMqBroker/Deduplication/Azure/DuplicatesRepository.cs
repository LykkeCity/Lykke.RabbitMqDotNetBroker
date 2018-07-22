using System.Threading.Tasks;
using AzureStorage;

namespace Lykke.RabbitMqBroker.Deduplication.Azure
{
    internal class DuplicatesRepository
    {
        private readonly INoSQLTableStorage<DuplicateEntity> _tableStorage;

        public DuplicatesRepository(INoSQLTableStorage<DuplicateEntity> tableStorage)
        {
            _tableStorage = tableStorage;
        }

        public Task AddAsync(byte[] value)
        {
            var entity = DuplicateEntity.Create(value);
            return _tableStorage.InsertOrMergeAsync(entity);
        }
        
        public Task<bool> DuplicateExistsAsync(byte[] value)
        {
            var entity = DuplicateEntity.Create(value);
            return _tableStorage.RecordExistsAsync(entity);
        }
    }
}
