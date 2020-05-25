using System;
using Autofac;

namespace Lykke.RabbitMqBroker
{
    /// <summary>
    /// Interface for reistering startable and stopable components
    /// </summary>
    public interface IStartStop: IStartable, IDisposable
    {
        /// <summary>
        /// Stops the components
        /// </summary>
        void Stop();
    }
}
