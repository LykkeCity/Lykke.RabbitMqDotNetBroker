// Copyright (c) Lykke Corp.
// Licensed under the MIT License. See the LICENSE file in the project root for more information.

using System.IO;
using JetBrains.Annotations;
using MessagePack;

namespace Lykke.RabbitMqBroker.Publisher.Serializers
{
    /// <summary>
    /// Uses MessagePack to serialize the message
    /// </summary>
    [PublicAPI]
    public class MessagePackMessageSerializer<TMessage> : IRabbitMqSerializer<TMessage>
    {
        private readonly IFormatterResolver _formatResolver;

        public MessagePackMessageSerializer(IFormatterResolver formatResolver = null)
        {
            _formatResolver = formatResolver;
        }

        public byte[] Serialize(TMessage model)
        {
            using (var stream = new MemoryStream())
            {
                MessagePackSerializer.Serialize(stream, model, _formatResolver);

                stream.Flush();

                return stream.ToArray();
            }
        }
    }
}
