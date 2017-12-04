using System.Text;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace Lykke.RabbitMqBroker.Subscriber
{
    [PublicAPI]
    public class JsonMessageDeserializer<TMessage> : IMessageDeserializer<TMessage>
    {
        private readonly Encoding _encoding;
        private readonly JsonSerializerSettings _settings;

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
            _settings = settings ?? new JsonSerializerSettings
            {
                DateTimeZoneHandling = DateTimeZoneHandling.Utc
            };
        }

        public TMessage Deserialize(byte[] data)
        {
            var settings = _settings ;
            var json = _encoding.GetString(data);

            return JsonConvert.DeserializeObject<TMessage>(json, settings);
        }
    }
}
