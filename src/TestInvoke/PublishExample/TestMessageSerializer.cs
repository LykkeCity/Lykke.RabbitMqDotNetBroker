using System.Text;
using Lykke.RabbitMqBroker.Publisher;

namespace TestInvoke.PublishExample
{
    public class TestMessageSerializer : IRabbitMqSerializer<string>
    {
        public byte[] Serialize(string model)
        {
            return Encoding.UTF8.GetBytes(model);
        }
    }
}
