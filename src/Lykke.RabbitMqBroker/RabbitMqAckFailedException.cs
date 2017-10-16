using System;

namespace Lykke.RabbitMqBroker
{
    [Serializable]
    public class RabbitMqAckFailedException : RabbitMqBrokerException
    {
        public RabbitMqAckFailedException(string message) :
            base(message)
        {
        }

        public RabbitMqAckFailedException(string message, Exception innterException) :
            base(message, innterException)
        {
        }
    }
}
