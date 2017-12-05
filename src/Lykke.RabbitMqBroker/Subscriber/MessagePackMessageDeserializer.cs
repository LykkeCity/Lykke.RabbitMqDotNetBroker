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
                stream.Position = 0;

                return MessagePackSerializer.Deserialize<TMessage>(stream, _formatterResolver, _readStrict);
            }
        }
    }
}
