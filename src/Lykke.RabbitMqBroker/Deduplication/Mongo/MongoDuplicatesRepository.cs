using System.Threading.Tasks;
using MongoDB.Driver;

namespace Lykke.RabbitMqBroker.Deduplication.Mongo
{
    public class MongoDuplicatesRepository
    {
        private readonly IMongoCollection<MongoDuplicateEntity> _collection;

        public MongoDuplicatesRepository(IMongoClient mongoClient, string tableName, string dbName = "maindb")
        {
            var db = mongoClient.GetDatabase(dbName);
            _collection = db.GetCollection<MongoDuplicateEntity>(tableName);
        }
        
        public async Task<bool> EnsureNotDuplicateAsync(byte[] value)
        {
            try
            {
                string id = HashHelper.GetMd5Hash(value);
                await _collection.InsertOneAsync(new MongoDuplicateEntity {BsonId = id});
                return true;
            }
            catch (MongoWriteException ex)
            {
                if (ex.Message.Contains("duplicate key error"))
                    return false;

                throw;
            }
        }
    }
}

