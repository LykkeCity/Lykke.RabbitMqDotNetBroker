// Copyright (c) Lykke Corp.
// Licensed under the MIT License. See the LICENSE file in the project root for more information.

using System.Text;
using JetBrains.Annotations;
using MessagePack;
using Newtonsoft.Json;

namespace Lykke.RabbitMqBroker.Subscriber.Deserializers
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
