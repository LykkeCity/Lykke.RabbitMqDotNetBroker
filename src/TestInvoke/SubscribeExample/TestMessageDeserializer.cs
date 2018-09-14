// Copyright (c) Lykke Corp.
// Licensed under the MIT License. See the LICENSE file in the project root for more information.

using System.Text;
using Lykke.RabbitMqBroker.Subscriber;

namespace TestInvoke.SubscribeExample
{
    public class TestMessageDeserializer : IMessageDeserializer<string>
    {
        public string Deserialize(byte[] data)
        {
            return Encoding.UTF8.GetString(data);
        }
    }
}
