// Copyright (c) Lykke Corp.
// Licensed under the MIT License. See the LICENSE file in the project root for more information.

using System.Text;
using Lykke.RabbitMqBroker.Logging;
using Lykke.RabbitMqBroker.Publisher.Serializers;

namespace TestInvoke.PublishExample
{
    public class TestMessageSerializer : IRabbitMqSerializer<string>
    {
        public byte[] Serialize(string model)
        {
            return Encoding.UTF8.GetBytes(model);
        }

        public SerializationFormat SerializationFormat { get; } = SerializationFormat.Unknown;
    }
}
