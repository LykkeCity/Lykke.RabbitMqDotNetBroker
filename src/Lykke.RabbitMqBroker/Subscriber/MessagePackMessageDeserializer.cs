// Copyright (c) Lykke Corp.
// Licensed under the MIT License. See the LICENSE file in the project root for more information.

using System.IO;
using JetBrains.Annotations;
using MessagePack;

namespace Lykke.RabbitMqBroker.Subscriber
{
    /// <summary>
    /// Uses MessagePack to deserialize the message
    /// </summary>
    [PublicAPI]
    public class MessagePackMessageDeserializer<TMessage> : IMessageDeserializer<TMessage>
    {
        private readonly IFormatterResolver _formatterResolver;
        private readonly bool _readStrict;

        /// <summary>
        /// Uses MessagePack to deserialize the message
        /// </summary>
        public MessagePackMessageDeserializer(IFormatterResolver formatterResolver = null, bool readStrict = false)
        {
            _formatterResolver = formatterResolver;
            _readStrict = readStrict;
        }

        public TMessage Deserialize(byte[] data)
        {
            using (var stream = new MemoryStream(data))
            {
                return MessagePackSerializer.Deserialize<TMessage>(stream, _formatterResolver, _readStrict);
            }
        }
    }
}
