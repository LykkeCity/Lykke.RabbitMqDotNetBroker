using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Log;
using Lykke.RabbitMqBroker;
using Lykke.RabbitMqBroker.Subscriber;

namespace TestInvoke.SubscribeExample
{
    public class HowToSubscribe
    {
        public static void Example(RabbitMqSettings settings)
        {

            var connector =
                new RabbitMqSubscriber<string>(settings)
                  .SetMessageDeserializer(new TestMessageDeserializer())
                  .SetMessageReadStrategy(new MessageReadWithTemporaryQueueStrategy())
                  .Subscribe(HandleMessage)
                  .SetLogger(new LogToConsole())
                  .Start();
        }

        private static Task HandleMessage(string msg)
        {
            Console.WriteLine(msg);
            return Task.FromResult(0);
        }
    }
}
