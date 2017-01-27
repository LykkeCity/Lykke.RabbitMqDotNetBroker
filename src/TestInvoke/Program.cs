using Common.Log;
using Lykke.RabbitMqBroker;
using System;
using System.Threading.Tasks;
using Lykke.RabbitMqBroker.Subscriber;
using TestInvoke.PublishExample;
using TestInvoke.SubscribeExample;

namespace TestInvoke
{
    public class Program
    {

        public static void Main(string[] args)
        {
            var rabbitMqSettings = new RabbitMqSettings
            {
                QueueName = Environment.GetEnvironmentVariable("RabbitMqQueue"),
                ConnectionString = Environment.GetEnvironmentVariable("RabbitMqConnectionString")
            };

            HowToPublish.Example(rabbitMqSettings);
            HowToSubscribe.Example(rabbitMqSettings);

            Console.WriteLine("Done");

            Console.ReadLine();
        }

    }
}
