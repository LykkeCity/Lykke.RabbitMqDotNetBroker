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

        public Task<bool> EnsureNotDuplicateAsync(byte[] value)
        {
            var entity = DuplicateEntity.Create(value);
            return _tableStorage.CreateIfNotExistsAsync(entity);
        }
    }
}
