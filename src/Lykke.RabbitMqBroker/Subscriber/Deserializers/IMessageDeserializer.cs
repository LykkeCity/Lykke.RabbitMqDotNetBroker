// Copyright (c) Lykke Corp.
// Licensed under the MIT License. See the LICENSE file in the project root for more information.

using JetBrains.Annotations;

namespace Lykke.RabbitMqBroker.Subscriber.Deserializers
{
    [PublicAPI]
    public interface IMessageDeserializer<out TModel>
    {
        TModel Deserialize(byte[] data);
    }
}
