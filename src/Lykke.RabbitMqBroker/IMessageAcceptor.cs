// Copyright (c) Lykke Corp.
// Licensed under the MIT License. See the LICENSE file in the project root for more information.

namespace Lykke.RabbitMqBroker
{
    public interface IMessageAcceptor
    {
        void Accept();
        void Reject();
    }
}