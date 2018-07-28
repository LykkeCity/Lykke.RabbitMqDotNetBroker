using System.Threading.Tasks;
using MongoDB.Driver;

namespace Lykke.RabbitMqBroker.Deduplication.Mongo
{
    /// <inheritdoc />
    /// <remarks>Mongo db implementation</remarks>
    public class MongoStorageDeduplicator : IDeduplicator
    {
        private MongoDuplicatesRepository _repository;
        private const string DbName = "MongoDeduplicator";

        public MongoStorageDeduplicator(IMongoClient mongoClient, string tableName)
        {
            _repository = new MongoDuplicatesRepository(mongoClient, DbName, tableName);
        }
        
        public Task<bool> EnsureNotDuplicateAsync(byte[] value)
        {
            return _repository.EnsureNotDuplicateAsync(value);
        }
    }
}
