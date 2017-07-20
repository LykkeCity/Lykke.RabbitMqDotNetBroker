namespace Lykke.RabbitMqBroker
{
    public interface IMessageAcceptor
    {
        void Accept();
        void Reject();
    }
}