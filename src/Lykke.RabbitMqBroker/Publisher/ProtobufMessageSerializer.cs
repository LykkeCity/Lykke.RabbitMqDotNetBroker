using System.IO;
using JetBrains.Annotations;

namespace Lykke.RabbitMqBroker.Publisher
{
    /// <summary>
    /// Uses Protobuf to serialize the message
    /// </summary>
    [PublicAPI]
    public class ProtobufMessageSerializer<TMessage> : IRabbitMqSerializer<TMessage>
    {
        public byte[] Serialize(TMessage model)
        {
            using (var stream = new MemoryStream())
            {
                ProtoBuf.Serializer.Serialize(stream, model);

                stream.Flush();

                return stream.ToArray();
            }
        }
    }
}
