using System;

namespace Lykke.RabbitMqBroker
{
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