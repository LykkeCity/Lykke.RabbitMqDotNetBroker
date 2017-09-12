namespace Lykke.RabbitMqBroker.Publisher
{
    public interface IRabbitMqSerializer<in TMessageModel>
    {
        byte[] Serialize(TMessageModel model);
    }
}