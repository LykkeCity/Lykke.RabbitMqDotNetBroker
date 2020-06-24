// Copyright (c) Lykke Corp.
// Licensed under the MIT License. See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Lykke.RabbitMqBroker;
using Lykke.RabbitMqBroker.Subscriber;
using Lykke.RabbitMqBroker.Subscriber.Middleware.ErrorHandling;
using Microsoft.Extensions.Logging.Abstractions;

namespace TestInvoke.SubscribeExample
{
    public static class HowToSubscribe
    {
        private static RabbitMqSubscriber<string> _connector;

        public static void Example(RabbitMqSubscriptionSettings settings)
        {
            _connector =
                new RabbitMqSubscriber<string>(
                    new NullLogger<RabbitMqSubscriber<string>>(),
                    settings)
                    .UseMiddleware(new ExceptionSwallowMiddleware<string>(new NullLogger<ExceptionSwallowMiddleware<string>>()))
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
