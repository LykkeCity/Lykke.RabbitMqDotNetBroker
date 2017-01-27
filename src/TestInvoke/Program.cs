using Common.Log;
using Lykke.RabbitMqBroker;
using System;
using System.Threading.Tasks;

namespace TestInvoke
{
    public class Program
    {
        private static RabbitMqBroker<string> _connector;

        public static void Main(string[] args)
        {
            var rabbitMqSettings = new RabbitMqSettings
            {
                ConnectionString = "",
                QueueName = ""
            };

            _connector = 
                new RabbitMqBroker<string>(rabbitMqSettings)
                  .SetMessageDeserializer(new TestMessageDeserializer())
                  .SetMessageReadStrategy(new MessageReadWithTemporaryQueueStrategy())
                  .Subscribe(HandleMessage)
                  .SetLogger(new LogToConsole())
                  .Start();

            Console.WriteLine("Started");

            Console.ReadLine();
        }
        private static Task HandleMessage(string msg)
        {
            Console.WriteLine(msg);
            return Task.FromResult(0);
        }
    }
}
