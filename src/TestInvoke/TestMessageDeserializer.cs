using Lykke.RabbitMqBroker;
using System.Text;

namespace TestInvoke
{
    public class TestMessageDeserializer : IMessageDeserializer<string>
    {
        public string Deserialize(byte[] data)
        {
            return Encoding.UTF8.GetString(data);
        }
    }
}
