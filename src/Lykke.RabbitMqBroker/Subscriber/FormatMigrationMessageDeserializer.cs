// Copyright (c) Lykke Corp.
// Licensed under the MIT License. See the LICENSE file in the project root for more information.

using System;
using JetBrains.Annotations;

namespace Lykke.RabbitMqBroker.Subscriber
{
    /// <summary>
    /// Deserializes messages of the type <typeparamref name="TMessage"/> which can be transmitted in two different formats.
    /// This deserializer applicable for consumers when producer will change format of the serialized message, 
    /// but not structure of the message itself.
    /// </summary>
    [PublicAPI]
    public class FormatMigrationMessageDeserializer<TMessage> : IMessageDeserializer<TMessage>
    {
        private readonly IMessageDeserializer<TMessage> _legacyFormatDeserializer;
        private readonly IMessageDeserializer<TMessage> _newFromatDeserializer;

        private IMessageDeserializer<TMessage> _primaryDeserializer;

        /// <summary>
        /// Deserializes messages of the type <typeparamref name="TMessage"/> which can be transmitted in two different formats.
        /// This deserializer applicable for consumers when producer will change format of the serialized message, 
        /// but not structure of the message itself.
        /// </summary>
        public FormatMigrationMessageDeserializer(
            IMessageDeserializer<TMessage> legacyFormatDeserializer,
            IMessageDeserializer<TMessage> newFromatDeserializer)
        {
            _legacyFormatDeserializer = legacyFormatDeserializer ?? throw new ArgumentNullException(nameof(legacyFormatDeserializer));
            _newFromatDeserializer = newFromatDeserializer ?? throw new ArgumentNullException(nameof(newFromatDeserializer));

            if (ReferenceEquals(_legacyFormatDeserializer, _newFromatDeserializer))
            {
                throw new InvalidOperationException("Legacy and new format deserializers should be different objects");
            }

            _primaryDeserializer = _newFromatDeserializer;
        }

        public TMessage Deserialize(byte[] data)
        {
            try
            {
                return _primaryDeserializer.Deserialize(data);
            }
            catch
            {
                // Switches to another deserializer and tries to deserialize

                _primaryDeserializer = ReferenceEquals(_primaryDeserializer, _newFromatDeserializer) 
                    ? _legacyFormatDeserializer 
                    : _newFromatDeserializer;

                return _primaryDeserializer.Deserialize(data);
            }
        }
    }
}
