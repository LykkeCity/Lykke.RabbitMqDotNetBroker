using JetBrains.Annotations;

namespace Lykke.RabbitMqBroker.Subscriber
{
    [PublicAPI]
    public interface IMessageDeserializer<out TModel>
    {
        TModel Deserialize(byte[] data);
    }
}
