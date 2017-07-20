using System;
using System.Threading.Tasks;
using Common;
using Common.Log;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Autofac;

namespace Lykke.RabbitMqBroker.Subscriber
{
    public interface IMessageDeserializer<out TModel>
    {
        TModel Deserialize(byte[] data);
    }

    public interface IMessageReadStrategy
    {
        string Configure(RabbitMqSubscribtionSettings settings, IModel channel);
    }

    public class RabbitMqSubscriber<TTopicModel> : IStartable, IStopable, IMessageConsumer<TTopicModel>
    {
        private Func<TTopicModel, Task> _eventHandler;
        private readonly RabbitMqSubscribtionSettings _rabbitMqSettings;
        private readonly IErrorHandlingStrategy _errorHandlingStrategy;
        private readonly int _reconnectTimeOut;
        private ILog _log;
        private Thread _thread;
        private IMessageDeserializer<TTopicModel> _messageDeserializer;
        private IMessageReadStrategy _messageReadStrategy;
        private IConsole _console;

        public RabbitMqSubscriber(RabbitMqSubscribtionSettings rabbitMqSettings, IErrorHandlingStrategy errorHandlingStrategy, int reconnectTimeOut = 3000)
        {
            _rabbitMqSettings = rabbitMqSettings;
            _errorHandlingStrategy = errorHandlingStrategy;
            _reconnectTimeOut = reconnectTimeOut;
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
            _eventHandler = callback;
            return this;
        }

        public RabbitMqSubscriber<TTopicModel> SetLogger(ILog log)
        {
            _log = log;
            return this;
        }

        public RabbitMqSubscriber<TTopicModel> SetConsole(IConsole console)
        {
            _console = console;
            return this;
        }

        public RabbitMqSubscriber<TTopicModel> SetMessageReadStrategy(IMessageReadStrategy messageReadStrategy)
        {
            _messageReadStrategy = messageReadStrategy;
            return this;
        }

        public RabbitMqSubscriber<TTopicModel> CreateDefaultBinding()
        {
            SetMessageReadStrategy(new MessageReadWithTemporaryQueueStrategy());
            return this;
        }

        #endregion

        void IMessageConsumer<TTopicModel>.Subscribe(Func<TTopicModel, Task> callback)
        {
            Subscribe(callback);
        }

        private bool IsStopped()
        {
            return _thread == null;
        }

        private void ReadThread()
        {
            while (!IsStopped())
                try
                {
                    ConnectAndReadAsync();
                }
                catch (Exception ex)
                {
                    _console?.WriteLine($"{_rabbitMqSettings.GetSubscriberName()}: ERROR: {ex.Message}");
                    _log?.WriteFatalErrorAsync(_rabbitMqSettings.GetSubscriberName(), "ReadThread", "", ex).Wait();
                }
                finally
                {
                    Thread.Sleep(_reconnectTimeOut);
                }
        }

        private void ConnectAndReadAsync()
        {
            var factory = new ConnectionFactory { Uri = _rabbitMqSettings.ConnectionString };
            _console?.WriteLine($"{_rabbitMqSettings.GetSubscriberName()}: trying to connect to {_rabbitMqSettings.ConnectionString} ({_rabbitMqSettings.GetQueueOrExchangeName()})");

            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                _console?.WriteLine($"{_rabbitMqSettings.GetSubscriberName()}:  connected to {_rabbitMqSettings.ConnectionString} ({_rabbitMqSettings.GetQueueOrExchangeName()})");

                var queueName = _messageReadStrategy.Configure(_rabbitMqSettings, channel);

                var consumer = new QueueingBasicConsumer(channel);
                string tag = channel.BasicConsume(queueName, false, consumer);

                //consumer.Received += MessageReceived;

                while (connection.IsOpen && !IsStopped())
                {
                    BasicDeliverEventArgs eventArgs;
                    var delivered = consumer.Queue.Dequeue(2000, out eventArgs);
                    if (delivered)
                    {
                        MessageReceived(eventArgs, channel);
                    }
                }

                channel.BasicCancel(tag);
                connection.Close();

                _console?.WriteLine($"{_rabbitMqSettings.GetSubscriberName()}: connection to {_rabbitMqSettings.ConnectionString} closed");
            }
        }

        private void MessageReceived(BasicDeliverEventArgs basicDeliverEventArgs, IModel channel)
        {
            try
            {
                var tag = basicDeliverEventArgs.DeliveryTag;
                var body = basicDeliverEventArgs.Body;
                var model = _messageDeserializer.Deserialize(body);

                var ma = new MessageAcceptor(channel, tag);

                _errorHandlingStrategy.Execute(() => _eventHandler(model).Wait(), ma);

            }
            catch (Exception ex)
            {
                _console?.WriteLine("Error in error handling strategy");
                _log?.WriteErrorAsync(GetType().Name, "Error in error handling strategy", "Message Receiving", ex);
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

            if (_eventHandler == null)
                throw new Exception("Please specify message handler");

            if (_messageReadStrategy == null)
                CreateDefaultBinding();

            if (_thread != null) return this;

            _thread = new Thread(ReadThread);
            _thread.Start();
            return this;
        }

        void IStopable.Stop()
        {
            Stop();
        }

        public void Stop()
        {
            var thread = _thread;

            if (thread == null)
                return;

            _thread = null;
            thread.Join();
        }
    }
}
