namespace Lykke.RabbitMqBroker.Publisher
{
    public interface IInMemoryQueuePublisher
    {
        int QueueSize { get; }
        string Name { get; }
    }
}