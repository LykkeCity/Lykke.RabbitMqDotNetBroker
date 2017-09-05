using System.Text;
using Newtonsoft.Json;

namespace Lykke.RabbitMqBroker.Publisher
{
    public class JsonMessageSerializer<TMessage> : IRabbitMqSerializer<TMessage>
    {
        private readonly Encoding _encoding;
        private readonly JsonSerializerSettings _settings;

        public JsonMessageSerializer() :
            this(Encoding.UTF8, null)
        {
        }

        public JsonMessageSerializer(Encoding encoding) :
            this(encoding, null)
        {
        }

        public JsonMessageSerializer(JsonSerializerSettings settings) :
            this(null, settings)
        {
        }

        public JsonMessageSerializer(Encoding encoding, JsonSerializerSettings settings)
        {
            _encoding = encoding;
            _settings = settings;
        }

        public byte[] Serialize(TMessage model)
        {
            var settings = _settings ?? new JsonSerializerSettings
            {
                DateTimeZoneHandling = DateTimeZoneHandling.Utc
            };
            var serialized = JsonConvert.SerializeObject(model, settings);

            return _encoding.GetBytes(serialized);
        }
    }
}