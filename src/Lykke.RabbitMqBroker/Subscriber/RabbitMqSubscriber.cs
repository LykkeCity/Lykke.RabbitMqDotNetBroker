using System;
using System.Threading.Tasks;
using Common;
using Common.Log;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Collections.Generic;
using System.Linq;
using Autofac;

namespace Lykke.RabbitMqBroker.Subscriber
{

    public interface IMessageDeserializer<out TModel>
    {
        TModel Deserialize(byte[] data);
    }

    public interface IMessageReadStrategy
    {
        string Configure(RabbitMqSettings settings, IModel channel);
    }


    public class RabbitMqSubscriber<TTopicModel> : IStartable, IMessageConsumer<TTopicModel>
    {

        private readonly List<Func<TTopicModel, Task>> _eventHandlers = new List<Func<TTopicModel, Task>>();
        private ILog _log;

        private IMessageDeserializer<TTopicModel> _messageDeserializer;

        private readonly RabbitMqSettings _rabbitMqSettings;


        private IMessageReadStrategy _messageReadStrategy;

        public RabbitMqSubscriber(RabbitMqSettings rabbitMqSettings)
        {
            _rabbitMqSettings = rabbitMqSettings;
        }

        #region Configurator

        public RabbitMqSubscriber<TTopicModel> SetMessageDeserializer(
            IMessageDeserializer<TTopicModel> messageDeserializer)
        {
            _messageDeserializer = messageDeserializer;
            return this;
        }

        public RabbitMqSubscriber<TTopicModel> Subscribe(Func<TTopicModel, Task> callback)
        {
            _eventHandlers.Add(callback);
            return this;
        }

        public RabbitMqSubscriber<TTopicModel> SetLogger(ILog log)
        {
            _log = log;
            return this;
        }

        public RabbitMqSubscriber<TTopicModel> SetMessageReadStrategy(IMessageReadStrategy messageReadStrategy)
        {
            _messageReadStrategy = messageReadStrategy;
            return this;
        }

        #endregion

        void IMessageConsumer<TTopicModel>.Subscribe(Func<TTopicModel, Task> callback)
        {
            Subscribe(callback);
        }

        private async void ReadThread()
        {

            while (true)
                try
                {
                    await ConnectAndReadAsync();

                }
                catch (Exception ex)
                {
                    if (_log != null)
                      await _log.WriteFatalErrorAsync("RabbitMQ " + _rabbitMqSettings.QueueName, "ReadThread", "", ex);
                }
                finally
                {
                    await Task.Delay(3000);
                }

        }



        private async Task ConnectAndReadAsync()
        {

            var factory = new ConnectionFactory {Uri = _rabbitMqSettings.ConnectionString};

            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {

                var queueName = _messageReadStrategy.Configure(_rabbitMqSettings, channel);

                var consumer = new EventingBasicConsumer(channel);

                consumer.Received += MessageReceived;
                channel.BasicConsume(queueName, true, consumer);

                while(connection.IsOpen){
                    await Task.Delay(2000);
                }

                consumer.Received -= MessageReceived;

                connection.Close();
            }
        }

        private void MessageReceived(object sender, BasicDeliverEventArgs basicDeliverEventArgs)
        {

            try
            {        
                var body = basicDeliverEventArgs.Body;
                var model = _messageDeserializer.Deserialize(body);
                Task.WhenAll(_eventHandlers.Select(eventHandler => eventHandler(model))).Wait();
            }
            catch (Exception ex)
            {
                _log?.WriteErrorAsync("RabbitMQ " + _rabbitMqSettings.QueueName, "Message Recieveing", "", ex);
            }

        }

        void IStartable.Start()
        {
            Start();
        }

        public RabbitMqSubscriber<TTopicModel> Start()
        {
            if (_messageDeserializer == null)
                throw new Exception("Please specify message deserializer");

            if (_eventHandlers.Count == 0)
                throw new Exception("Please specify message handler");

            if (_messageReadStrategy == null)
                throw new Exception("Please specify message read strategy");

            ReadThread();
            return this;
        }

    }

}
