using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Common;
using Common.Log;
using RabbitMQ.Client;

namespace Lykke.RabbitMqBroker.Publisher
{

    public interface IRabbitMqSerializer<in TMessageModel>
    {
        byte[] Serialize(TMessageModel model);
    }

    public interface IRabbitMqPublishStrategy
    {
        void Configure(RabbitMqSettings settings, IModel channel);
        void Publish(RabbitMqSettings settings, IModel channel, byte[] data);
    }

    public class RabbitMqPublisher<TMessageModel> : IMessageProducer<TMessageModel>, IStartable, IStopable
    {
        private readonly RabbitMqSettings _settings;
        private IRabbitMqSerializer<TMessageModel> _serializer;
        private ILog _log;
        private IRabbitMqPublishStrategy _publishStrategy;

        public RabbitMqPublisher(RabbitMqSettings settings)
        {
            _settings = settings;
        }

        #region MyRegion

        public RabbitMqPublisher<TMessageModel> SetSerializer(IRabbitMqSerializer<TMessageModel> serializer)
        {
            _serializer = serializer;
            return this;
        }

        public RabbitMqPublisher<TMessageModel> SetPublishStrategy(IRabbitMqPublishStrategy publishStrategy)
        {
            _publishStrategy = publishStrategy;
            return this;
        }

        public RabbitMqPublisher<TMessageModel> SetLogger(ILog log)
        {
            _log = log;
            return this;
        }
        #endregion


        private readonly Queue<TMessageModel> _items = new Queue<TMessageModel>();

        public Task ProduceAsync(TMessageModel message)
        {
            lock (_items)
                _items.Enqueue(message);
            return Task.FromResult(0);
        }

        private Thread _thread;

        public RabbitMqPublisher<TMessageModel> Start()
        {
            if (_thread == null)
            {
                _thread = new Thread(ConnectionThread);
                _thread.Start();
            }

            return this;
        }

        void IStartable.Start()
        {
            Start();
        }

        void IStopable.Stop()
        {
            var thread = _thread;

            if (thread == null)
                return;

            if (_serializer == null)
                throw new Exception("RabbitMQPublisher serializer is not specified");

            if (_publishStrategy == null)
                _publishStrategy = new DefaultFnoutPublishStrategy();

            _thread = null;
            thread.Join();
        }

        private bool IsStopped()
        {
            return _thread == null;
        }

        private TMessageModel EnqueueMessage()
        {
            lock (_items)
            {
                if (_items.Count > 0)
                    return _items.Dequeue();
            }

            return default(TMessageModel);
        }

        private void ConnectAndRead()
        {
            var factory = new ConnectionFactory { Uri = _settings.ConnectionString };

            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                _publishStrategy.Configure(_settings, channel);

                while (true)
                {
                    if (!connection.IsOpen)
                        throw new Exception("Connection is closed");

                    var message = EnqueueMessage();

                    if (message == null)
                    {
                        if (IsStopped())
                            return;

                        Thread.Sleep(300);
                        continue;
                    }

                    var body = _serializer.Serialize(message);
                    _publishStrategy.Publish(_settings, channel, body);
                }
            }
        }

        private void ConnectionThread()
        {
            while (!IsStopped())
            {
                try
                {
                    ConnectAndRead();
                }
                catch (Exception e)
                {
                    _log?.WriteErrorAsync("QueueProducer " + _settings.QueueName, "ConnectionThread", "", e).Wait();
                }
            }
        }
    }
}
