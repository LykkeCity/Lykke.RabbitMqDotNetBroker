using System.Text;
using Newtonsoft.Json;

namespace Lykke.RabbitMqBroker.Subscriber
{
    public class JsonMessageDeserializer<TMessage> : IMessageDeserializer<TMessage>
    {
        private readonly Encoding _encoding;
        private readonly JsonSerializerSettings _settings;

        public JsonMessageDeserializer() :
            this(Encoding.UTF8, null)
        {
        }

        public JsonMessageDeserializer(Encoding encoding) :
            this(encoding, null)
        {
        }

        public JsonMessageDeserializer(JsonSerializerSettings settings) :
            this(Encoding.UTF8, settings)
        {
        }

        public JsonMessageDeserializer(Encoding encoding, JsonSerializerSettings settings)
        {
            _encoding = encoding;
            _settings = settings;
        }

        public TMessage Deserialize(byte[] data)
        {
            var settings = _settings ?? new JsonSerializerSettings
            {
                DateTimeZoneHandling = DateTimeZoneHandling.Utc
            };
            var json = _encoding.GetString(data);

            return JsonConvert.DeserializeObject<TMessage>(json, settings);
        }
    }
}