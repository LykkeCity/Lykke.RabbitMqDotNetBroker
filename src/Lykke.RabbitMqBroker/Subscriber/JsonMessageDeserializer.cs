using System.Text;
using Newtonsoft.Json;

namespace Lykke.RabbitMqBroker.Subscriber
{
    public class JsonMessageDeserializer<TMessage> : IMessageDeserializer<TMessage>
    {
        private readonly Encoding _encoding;

        public JsonMessageDeserializer() :
            this(Encoding.UTF8)
        {
        }

        public JsonMessageDeserializer(Encoding encoding)
        {
            _encoding = encoding;
        }

        public TMessage Deserialize(byte[] data)
        {
            var json = _encoding.GetString(data);

            return JsonConvert.DeserializeObject<TMessage>(json);
        }
    }
}