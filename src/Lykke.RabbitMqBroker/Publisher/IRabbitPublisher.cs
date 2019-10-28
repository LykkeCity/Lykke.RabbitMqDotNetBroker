using System.Threading.Tasks;
using JetBrains.Annotations;
using Lykke.Common;

namespace Lykke.RabbitMqBroker.Publisher
{
    /// <summary>
    /// Interface for rabbitMq message publisher.
    /// </summary>
    [PublicAPI]
    public interface IRabbitPublisher<in TMessage> : IStartStop
    {
        /// <summary>
        /// Publishes a message to rabbitMq exchange.
        /// </summary>
        /// <param name="message">Message to be published.</param>
        Task PublishAsync(TMessage message);
    }
}
