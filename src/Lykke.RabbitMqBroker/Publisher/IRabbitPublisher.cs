using System.Threading.Tasks;
using Autofac;
using Common;
using JetBrains.Annotations;

namespace Lykke.RabbitMqBroker.Publisher
{
    /// <summary>
    /// Interface for 
    /// </summary>
    [PublicAPI]
    public interface IRabbitPublisher<in TMessage> : IStartable, IStopable
    {
        Task PublishAsync(TMessage message);
    }
}
