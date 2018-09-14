// Copyright (c) Lykke Corp.
// Licensed under the MIT License. See the LICENSE file in the project root for more information.

using System;

namespace Lykke.RabbitMqBroker
{
    [Serializable]
    public class RabbitMqBrokerException : Exception
    {
        public RabbitMqBrokerException(string message) :
            base(message)
        {
        }

        public RabbitMqBrokerException(string message, Exception innerException) :
            base(message, innerException)
        {
        }
    }
}
