using System.Text;
using Newtonsoft.Json;

namespace Lykke.RabbitMqBroker.Publisher
{
    public class JsonMessageSerializer<TMessage> : IRabbitMqSerializer<TMessage>
    {
        private readonly Encoding _encoding;

        public JsonMessageSerializer() :
            this(Encoding.UTF8)
        {
        }

        public JsonMessageSerializer(Encoding encoding)
        {
            _encoding = encoding;
        }

        public byte[] Serialize(TMessage model)
        {
            var serialized = JsonConvert.SerializeObject(model);

            return _encoding.GetBytes(serialized);
        }
    }
}