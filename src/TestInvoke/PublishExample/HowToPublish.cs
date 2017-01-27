using System;
using Lykke.RabbitMqBroker;
using Lykke.RabbitMqBroker.Publisher;

namespace TestInvoke.PublishExample
{
    public static class HowToPublish
    {
        public static void Example(RabbitMqSettings settings)
        {
            var rabbitMqSettings = new RabbitMqSettings
            {
                ConnectionString = "",
                QueueName = ""
            };

            var connection
                = new RabbitMqPublisher<string>(rabbitMqSettings)
                .SetSerializer(new TestMessageSerializer())
                .Start();
        }

    }
}
