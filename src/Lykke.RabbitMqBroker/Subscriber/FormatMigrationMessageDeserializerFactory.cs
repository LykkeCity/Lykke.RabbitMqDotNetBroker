using System.Text;
using JetBrains.Annotations;
using MessagePack;
using Newtonsoft.Json;

namespace Lykke.RabbitMqBroker.Subscriber
{
    /// <summary>
    /// Factory of the typical format migration message deserializers
    /// </summary>
    [PublicAPI]
    public static class FormatMigrationMessageDeserializerFactory
    {
        /// <summary>
        /// Creates <see cref="FormatMigrationMessageDeserializer{TMessage}"/> for migration from the Json to the MessagePack
        /// </summary>
        public static FormatMigrationMessageDeserializer<TMessage> JsonToMessagePack<TMessage>(
            Encoding jsonEncoding = null,
            JsonSerializerSettings jsonSettings = null,
            IFormatterResolver messagePackFormatterResolver = null)
        {
            return new FormatMigrationMessageDeserializer<TMessage>(
                new JsonMessageDeserializer<TMessage>(jsonEncoding, jsonSettings),
                new MessagePackMessageDeserializer<TMessage>(messagePackFormatterResolver));
        }
    }
}
