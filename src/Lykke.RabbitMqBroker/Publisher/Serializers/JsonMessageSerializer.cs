// Copyright (c) Lykke Corp.
// Licensed under the MIT License. See the LICENSE file in the project root for more information.

using System.Text;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace Lykke.RabbitMqBroker.Publisher.Serializers
{
    [PublicAPI]
    public class JsonMessageSerializer<TMessage> : IRabbitMqSerializer<TMessage>
    {
        private readonly Encoding _encoding;
        private readonly JsonSerializerSettings _settings;

        public JsonMessageSerializer() :
            this(null, null)
        {
        }

        public JsonMessageSerializer(Encoding encoding) :
            this(encoding, null)
        {
        }

        public JsonMessageSerializer(JsonSerializerSettings settings) :
            this(null, settings)
        {
        }

        public JsonMessageSerializer(Encoding encoding, JsonSerializerSettings settings)
        {
            _encoding = encoding ?? Encoding.UTF8;
            _settings = settings ?? new JsonSerializerSettings
            {
                DateTimeZoneHandling = DateTimeZoneHandling.Utc
            };
        }

        public byte[] Serialize(TMessage model)
        {
            var serialized = JsonConvert.SerializeObject(model, _settings);

            return _encoding.GetBytes(serialized);
        }
    }
}
