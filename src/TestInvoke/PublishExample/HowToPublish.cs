// Copyright (c) Lykke Corp.
// Licensed under the MIT License. See the LICENSE file in the project root for more information.

using Lykke.RabbitMqBroker.Publisher;
using Lykke.RabbitMqBroker.Subscriber;

namespace TestInvoke.PublishExample
{
    public static class HowToPublish
    {
        public static void Example(RabbitMqSubscriptionSettings settings)
        {

            var connection
                = new RabbitMqPublisher<string>(settings)
                .SetSerializer(new TestMessageSerializer())
                .SetPublishStrategy(new DefaultFanoutPublishStrategy(settings))
                .Start();


            for (var i = 0; i <= 10; i++)
                connection.ProduceAsync("message#" + i);
        }

    }
}
