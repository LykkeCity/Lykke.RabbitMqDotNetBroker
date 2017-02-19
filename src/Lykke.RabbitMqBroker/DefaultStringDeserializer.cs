using System.Text;
using Lykke.RabbitMqBroker.Subscriber;

namespace Lykke.RabbitMqBroker
{
    public class DefaultStringDeserializer : IMessageDeserializer<string>
    {
        public string Deserialize(byte[] data)
        {
            return Encoding.UTF8.GetString(data);
        }
    }
}
