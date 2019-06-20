using System.Threading.Tasks;
using Autofac;
using Common;
using JetBrains.Annotations;

namespace Lykke.RabbitMqBroker.Publisher
{
    /// <summary>
    /// Interface for rabbitMq message publisher.
    /// </summary>
    [PublicAPI]
    public interface IRabbitPublisher<in TMessage> : IStartable, IStopable
    {
        /// <summary>
        /// Publishes a message to rabbitMq exchange.
        /// </summary>
        /// <param name="message">Message to be published.</param>
        Task PublishAsync(TMessage message);
    }
}
