using System;
using System.Threading.Tasks;
using Common.Log;
using Lykke.RabbitMqBroker.Subscriber;

namespace TestInvoke.SubscribeExample
{
    public class HowToSubscribe
    {
        private static RabbitMqSubscriber<string> _connector;
        public static void Example(RabbitMqSubscriberSettings settings)
        {

            _connector =
                new RabbitMqSubscriber<string>(settings)
                  .SetMessageDeserializer(new TestMessageDeserializer())
                  .SetMessageReadStrategy(new MessageReadWithTemporaryQueueStrategy())
                  .Subscribe(HandleMessage)
                  .SetLogger(new LogToConsole())
                  .Start();
        }

        public static void Stop()
        {
            _connector.Stop();
        }

        private static Task HandleMessage(string msg)
        {
            Console.WriteLine(msg);
            return Task.FromResult(0);
        }
    }
}
