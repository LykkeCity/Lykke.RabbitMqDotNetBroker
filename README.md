# Lykke.RabbitMqDotNetBroker

Use this library to communicate using abstraction Publisher/Subscriber via RabbitMQ.

This Implementation support reconnection management. 

The basic patter to use this implementation:
Create, configure, start - everything works. 
If connection is lost - system reconnects automatically
If there is an exeption during any process of message handling - Lykke log system will inform IT support services to investigate the case.


To Use as message consumer (message broker) - answer these questions:

 - Which RabbitMQ server do you want to be connected using notation: https://www.rabbitmq.com/uri-spec.html 
 - How do you want deserealize array of bytes to your model: implement your IMessageDeserializer<TModel> interface and confugre it
 - Which RabbitMQ strategy do you want to use. Use whichever we already have or feel free to write your own IMessageReadStrategy and do pull request;
 - Specify Lykke Logging system: ILog;
 - Specify callback method for messages to be delivered;
 - Run the Broker;
 
 Example:
 ```csharp
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
```
