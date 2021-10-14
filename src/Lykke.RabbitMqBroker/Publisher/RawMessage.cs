// Copyright (c) Lykke Corp.
// Licensed under the MIT License. See the LICENSE file in the project root for more information.

using System.Collections.Generic;
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
        /// Message headers
        /// </summary>
        public IDictionary<string, object> Headers { get; }

        /// <summary>
        /// Raw (serialized) message with auxiliary message properties
        /// </summary>
        public RawMessage(byte[] body, string routingKey, IDictionary<string, object> headers)
        {
            Body = body;
            RoutingKey = routingKey;
            Headers = headers;
        }
    }
}
