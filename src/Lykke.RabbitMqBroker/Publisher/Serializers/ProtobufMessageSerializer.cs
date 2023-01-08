// Copyright (c) Lykke Corp.
// Licensed under the MIT License. See the LICENSE file in the project root for more information.

using System.IO;
using JetBrains.Annotations;
using Lykke.RabbitMqBroker.Logging;

namespace Lykke.RabbitMqBroker.Publisher.Serializers
{
    /// <summary>
    /// Uses Protobuf to serialize the message
    /// </summary>
    [PublicAPI]
    public class ProtobufMessageSerializer<TMessage> : IRabbitMqSerializer<TMessage>
    {
        /// <inheritdoc />
        public byte[] Serialize(TMessage model)
        {
            using (var stream = new MemoryStream())
            {
                ProtoBuf.Serializer.Serialize(stream, model);
                stream.Flush();
                return stream.ToArray();
            }
        }

        public SerializationFormat SerializationFormat { get; } = SerializationFormat.Protobuf;
    }
}
