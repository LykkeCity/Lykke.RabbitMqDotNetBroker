using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Logs;
using Lykke.RabbitMqBroker;
using Lykke.RabbitMqBroker.Subscriber;
using Microsoft.Extensions.Configuration;

namespace DeduplicationSubscriberTest
{
    class Program
    {
        private static bool _working = true;
        
        static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("Specify number of subscribers");
                return;
            }

            if (!int.TryParse(args[0], out var subscribersCount))
            {
                Console.WriteLine("Invalid subscribers count value");
                return;
            }

            if (subscribersCount <= 0)
            {
                Console.WriteLine("Specify at least 1 subscriber");
                return;
            }
            
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();
            
            var appSettings = new AppSettings();
            
            config.Bind(appSettings);
            
            // arrange
            var settings = new RabbitMqSubscriptionSettings
            {
                ConnectionString = appSettings.ConnString,
                ExchangeName = appSettings.ExchangeName,
                IsDurable = true,
                QueueName = appSettings.QueueName
            };

            var subscribers = new List<RabbitMqSubscriber<string>>();
            
            var handler = new Func<string, Task>(s =>
            {
                //Console.Write($"processing: {s}\r");
                return Task.CompletedTask;
            });

            for (var i = 0; i < subscribersCount; i++)
            {
                var subscriber = new RabbitMqSubscriber<string>(
                        EmptyLogFactory.Instance,
                        settings,
                        new DefaultErrorHandlingStrategy(EmptyLogFactory.Instance, settings))
                    .SetMessageDeserializer(new DefaultStringDeserializer())
                    .SetMessageReadStrategy(new MessageReadQueueStrategy())
                    .SetAlternativeExchange(appSettings.AlternateConnString)
                    .SetMongoDbStorageDeduplicator(appSettings.MongoDbConnString, "Duplicates", EmptyLogFactory.Instance)
//                    .SetHeaderDeduplication(appSettings.Header)
//                    .SetAzureStorageDeduplicator(appSettings.AzureConnString, "DepuplicationTest", EmptyLogFactory.Instance)
                    .Subscribe(handler)
                    .CreateDefaultBinding();
                
                subscribers.Add(subscriber);
            }

            try
            {
                Console.WriteLine($"Starting {subscribersCount} subscribers...");
                foreach (var subscriber in subscribers)
                    subscriber.Start();

                Console.WriteLine("Started. Press any key to exit...");
                Console.ReadKey(false);

                Console.WriteLine($"Stopping {subscribersCount} subscribers...");
                foreach (var subscriber in subscribers)
                    subscriber.Stop();
                
                Console.WriteLine("Stopped");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
