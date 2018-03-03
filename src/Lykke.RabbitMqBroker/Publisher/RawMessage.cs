using JetBrains.Annotations;

namespace Lykke.RabbitMqBroker.Publisher
{
    /// <summary>
    /// Raw (serialized) message with auxiliary message properties
    /// </summary>
    [PublicAPI]
    public class RawMessage
    {
        /// <summary>
        /// Serialied message
        /// </summary>
        public byte[] Body { get; }

        /// <summary>
        /// Message routing key
        /// </summary>
        public string RoutingKey { get; }

        /// <summary>
        /// Raw (serialized) message with auxiliary message properties
        /// </summary>
        public RawMessage(byte[] body, string routingKey)
        {
            Body = body;
            RoutingKey = routingKey;
        }
    }
}
