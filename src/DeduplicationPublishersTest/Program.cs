using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;

namespace DeduplicationPublishersTest
{
    class Program
    {
        private static bool _working = true;
        
        static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("Specify messages rate per second");
                return;
            }

            if (!int.TryParse(args[0], out var messagesRatePerSecond))
            {
                Console.WriteLine("Invalid messages rate per second value");
                return;
            }

            if (messagesRatePerSecond <= 0)
            {
                Console.WriteLine("Specify rate > 0");
                return;
            }
            
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();
            
            var appSettings = new AppSettings();
            
            config.Bind(appSettings);
            
            Console.Clear();
            int delay = 1000 / messagesRatePerSecond;
            WriteLineToPosition($"Press any key to stop publishers ({messagesRatePerSecond} rps, delay {delay} ms)", 0);
            try
            {
                Task.Run(() => SendMessages(appSettings.ConnString, appSettings.ExchangeName, appSettings.Header, 1, "Rabbit1", delay));
//                Task.Delay(100).Wait();
//                Task.Run(() => SendMessages(appSettings.AlternateConnString, appSettings.ExchangeName, appSettings.Header, 2, "Rabbit2", delay));

                Console.ReadKey(false);
                _working = false;
                WriteLineToPosition("Finished", 3, "Subscriber");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private static void SendMessages(string connString, string exchangeName, string header, int index, string prefix, int delay)
        {
            while (_working)
            {
                try
                {
                    WriteLineToPosition($"connecting to {connString}", index, prefix);
                    var factory = new ConnectionFactory {Uri = connString};
                    using (var connection = factory.CreateConnection())
                    using (var channel = connection.CreateModel())
                    {
                        if (connection.IsOpen)
                            WriteLineToPosition($"Connected to {connString}", index, prefix);
                        
                        connection.ConnectionShutdown += (sender, args) =>
                        {
                            WriteLineToPosition($"Shutdown connection to {connString}", index, prefix);
                        };

                        int i = 0;
                        
                        while (_working)
                        {
                            IBasicProperties basicProperties = null;

                            if (!string.IsNullOrEmpty(header))
                            {
                                basicProperties = channel.CreateBasicProperties();
                                basicProperties.Headers = new Dictionary<string, object>()
                                {
                                    {header, i}
                                };
                            }

                            WriteLineToPosition($"Sending {(string.IsNullOrEmpty(header) ? null : "header")} {i}", index, prefix);
                            channel.BasicPublish(exchangeName, "", body: Encoding.UTF8.GetBytes(i.ToString()),
                                basicProperties:
                                basicProperties);
                            Thread.Sleep(TimeSpan.FromMilliseconds(delay));
                            i++;
                        }
                    }
                }
                catch
                {
                    WriteLineToPosition("Error!", index, prefix);
                }
            }
            
            WriteLineToPosition("Exit", index, prefix);
        }

        private static void WriteLineToPosition(string str, int index, string prefix = null)
        {
            lock (Console.Out)
            {
                ClearCurrentConsoleLine(index);
                Console.WriteLine(string.IsNullOrEmpty(prefix) ? str : $"{prefix}: {str}");
            }
        }
        
        private static void ClearCurrentConsoleLine(int index)
        {
            Console.SetCursorPosition(0, index);
            Console.Write(new string(' ', Console.WindowWidth)); 
            Console.SetCursorPosition(0, index);
        }
    }
}
