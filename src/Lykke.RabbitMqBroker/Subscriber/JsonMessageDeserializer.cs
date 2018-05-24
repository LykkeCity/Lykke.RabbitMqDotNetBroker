using System.IO;
using System.Text;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace Lykke.RabbitMqBroker.Subscriber
{
    [PublicAPI]
    public class JsonMessageDeserializer<TMessage> : IMessageDeserializer<TMessage>
    {
        private readonly Encoding _encoding;
        private readonly JsonSerializer _serializer;

        public JsonMessageDeserializer() :
            this(null, null)
        {
        }

        public JsonMessageDeserializer(Encoding encoding) :
            this(encoding, null)
        {
        }

        public JsonMessageDeserializer(JsonSerializerSettings settings) :
            this(null, settings)
        {
        }

        public JsonMessageDeserializer(Encoding encoding, JsonSerializerSettings settings)
        {
            _encoding = encoding ?? Encoding.UTF8;
            _serializer = JsonSerializer.Create(settings ?? new JsonSerializerSettings
            {
                DateTimeZoneHandling = DateTimeZoneHandling.Utc
            });
        }

        public TMessage Deserialize(byte[] data)
        {
            using (var stream = new MemoryStream(data))
            using (var reader = new StreamReader(stream, _encoding))
            using (var jsonReader = new JsonTextReader(reader))
            {
                return _serializer.Deserialize<TMessage>(jsonReader);
            }
        }
    }
}
