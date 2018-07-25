using MongoDB.Bson.Serialization.Attributes;

namespace Lykke.RabbitMqBroker.Deduplication.Mongo
{
    public class MongoDuplicateEntity
    {
        [BsonId]
        public string BsonId { get; set; }

    }
}
