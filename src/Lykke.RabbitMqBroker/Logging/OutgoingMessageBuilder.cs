using System;
using System.Collections.Generic;

namespace Lykke.RabbitMqBroker.Logging
{
    public class OutgoingMessageBuilder
    {
        private readonly RabbitMqSubscriptionSettings _settings;
        private SerializationFormat _serializationFormat;

        public OutgoingMessageBuilder(RabbitMqSubscriptionSettings settings, SerializationFormat serializationFormat)
        {
            _settings = settings;
            _serializationFormat = serializationFormat;
        }

        public OutgoingMessageBuilder(RabbitMqSubscriptionSettings settings) : this(settings,
            SerializationFormat.Unknown)
        {
        }

        public void SetSerializationFormat(SerializationFormat format)
        {
            _serializationFormat = format;
        }

        public OutgoingMessage Create<TMessage>(byte[] serializedMessage, IDictionary<string, object> headers)
        {
            var type = typeof(TMessage);
            
            var message = new OutgoingMessage
            {
                MessageTypeName = type.Name,
                MessageTypeFullName = type.FullName,
                Exchange = _settings.ExchangeName,
                RoutingKey = _settings.RoutingKey,
                Format = _serializationFormat,
                Headers = headers == null
                    ? new Dictionary<string, object>()
                    : new Dictionary<string, object>(headers),
                Timestamp = DateTime.UtcNow,
                Message = Convert.ToBase64String(serializedMessage),
            };

            return message;
        }
    }
}
