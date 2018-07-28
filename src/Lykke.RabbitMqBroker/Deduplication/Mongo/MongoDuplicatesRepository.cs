using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Lykke.RabbitMqBroker.Deduplication.Mongo
{
    public class MongoDuplicatesRepository
    {
        private IMongoCollection<MongoDuplicateEntity> _collection;

        public MongoDuplicatesRepository(IMongoClient mongoClient, string dbName, string collectionName)
        {
            ConfigureShard(mongoClient, dbName, collectionName);
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

        private void ConfigureShard(IMongoClient dbClient, string dbName, string collectionName)
        {
            IMongoDatabase admin = dbClient.GetDatabase("admin");
            IMongoDatabase db = dbClient.GetDatabase(dbName);
            
            admin.RunCommand(new BsonDocumentCommand<BsonDocument>(new BsonDocument("enableSharding", dbName)));

            bool collectionExists = CollectionExists(db, collectionName);

            if (!collectionExists)
            {
                //Create collection with sharding
                admin.RunCommand(new BsonDocumentCommand<BsonDocument>(new BsonDocument
                {
                    {"shardCollection", $"{dbName}.{collectionName}"},
                    {"key", new BsonDocument{{"_id", "hashed"}}}
                }));
            }
            
            _collection = db.GetCollection<MongoDuplicateEntity>(collectionName);
            
            if (!collectionExists)
            {
                //Creating Hashed index to spread documents evenly across shards
                _collection.Indexes.CreateOne(new CreateIndexModel<MongoDuplicateEntity>(
                    Builders<MongoDuplicateEntity>.IndexKeys.Ascending(x => x.BsonId)));
            }
        }
        
        private bool CollectionExists(IMongoDatabase db, string collectionName)
        {
            var filter = new BsonDocument("name", collectionName);
            var collections = db.ListCollections(new ListCollectionsOptions { Filter = filter });
            return collections.Any();
        }
    }
}

