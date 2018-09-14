// Copyright (c) Lykke Corp.
// Licensed under the MIT License. See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Common.Log;
using Lykke.RabbitMqBroker;
using Lykke.RabbitMqBroker.Subscriber;

namespace TestInvoke.SubscribeExample
{
    public static class HowToSubscribe
    {
        private static RabbitMqSubscriber<string> _connector;
        public static void Example(RabbitMqSubscriptionSettings settings)
        {
            var looger = new LogToConsole();

            _connector =
                new RabbitMqSubscriber<string>(settings, new DefaultErrorHandlingStrategy(looger, settings))
                  .SetMessageDeserializer(new TestMessageDeserializer())
                  .CreateDefaultBinding()
                  .Subscribe(HandleMessage)
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
