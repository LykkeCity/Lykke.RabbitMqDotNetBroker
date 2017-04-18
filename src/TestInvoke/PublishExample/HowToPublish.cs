using Lykke.RabbitMqBroker.Publisher;

namespace TestInvoke.PublishExample
{
    public static class HowToPublish
    {
        public static void Example(RabbitMqPublisherSettings settings)
        {


            var connection
                = new RabbitMqPublisher<string>(settings)
                .SetSerializer(new TestMessageSerializer())
                .Start();


            for (var i = 0; i <= 10; i++)
                connection.ProduceAsync("message#" + i);
        }

    }
}
