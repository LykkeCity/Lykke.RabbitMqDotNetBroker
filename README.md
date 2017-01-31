[![Build status](https://ci.appveyor.com/api/projects/status/dco9jgns37uw87b1?svg=true)](https://ci.appveyor.com/project/lykke/lykke-rabbitmqdotnetbroker) - Last Build


[![Build status](https://ci.appveyor.com/api/projects/status/dco9jgns37uw87b1/branch/master?svg=true)](https://ci.appveyor.com/project/lykke/lykke-rabbitmqdotnetbroker/branch/master) - Master


# Lykke.RabbitMqDotNetBroker

Use this library to communicate using abstraction Publisher/Subscriber via RabbitMQ.

The basic pattern to use this implementation is: create, configure, start - everything works. 
If connection is lost - system reconnects automatically
If there is an exeption during any process of message handling - Lykke log system will inform IT support services to investigate the case.
We use one exhange/fnout - one type of contract model

# How Publish 

If you want to use a publisher partten answer next questions:
 - Which RabbitMQ server do you want to be connected using notation: https://www.rabbitmq.com/uri-spec.html 
 - How do you want serealize your model to array of bytes: implement your IMessageSerializer<TModel> interface and confugre it
 - Which RabbitMQ publish strategy do you want to use. Use whichever we already have or feel free to write your own IMessagePublishStrategy and do pull request; https://www.rabbitmq.com/tutorials/tutorial-three-dotnet.html
 - Specify Lykke Logging system: ILog;
 - Start the publisher;
 - Publish, publish, publish
 
 
 Example:
```csharp
    public static class HowToPublish
    {
        public static void Example(RabbitMqSettings settings)
        {
            var rabbitMqSettings = new RabbitMqSettings
            {
                ConnectionString = "",
                QueueName = ""
            };

            var connection
                = new RabbitMqPublisher<string>(rabbitMqSettings)
                .SetSerializer(new TestMessageSerializer())
                .Start();


            for (var i = 0; i <= 10; i++)
                connection.ProduceAsync("message#" + i);
        }

    }
```



# How to subscribe and consume

To consume messages produced by publisher - answer these questions:

 - Which RabbitMQ server do you want to be connected using notation: https://www.rabbitmq.com/uri-spec.html 
 - How do you want deserealize array of bytes to your model: implement your IMessageDeserializer<TModel> interface and confugre it
 - Which RabbitMQ strategy do you want to use. Use whichever we already have or feel free to write your own IMessageReadStrategy and do pull request; https://www.rabbitmq.com/tutorials/tutorial-three-dotnet.html
 - Specify Lykke Logging system: ILog;
 - Specify callback method for messages to be delivered;
 - Run the Broker;
 
 Example:
 ```csharp
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
```
