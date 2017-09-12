using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Common;
using Common.Log;
using Lykke.RabbitMqBroker.Subscriber;
using RabbitMQ.Client;

namespace Lykke.RabbitMqBroker.Publisher
{
    public class RabbitMqPublisher<TMessageModel> : IMessageProducer<TMessageModel>, IStartable, IStopable
    {
        private readonly RabbitMqSubscriptionSettings _settings;
        private IPublishingQueueRepository<TMessageModel> _queueRepository;
        private readonly Queue<TMessageModel> _items = new Queue<TMessageModel>();
        private Thread _thread;
        private IRabbitMqSerializer<TMessageModel> _serializer;
        private ILog _log;
        private IConsole _console;
        private IRabbitMqPublishStrategy _publishStrategy;
        private int _reconnectionsInARowCount;
             
        public RabbitMqPublisher(RabbitMqSubscriptionSettings settings)
        {
            _settings = settings;
        }

        #region Configurator

        /// <summary>
        /// Sets repository, which is used to save and load in-memory messages queue while starting and stopping 
        /// </summary>
        public RabbitMqPublisher<TMessageModel> SetQueueRepository(IPublishingQueueRepository<TMessageModel> queueRepository)
        {
            _queueRepository = queueRepository;

            return this;
        }

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

        public RabbitMqPublisher<TMessageModel> SetConsole(IConsole console)
        {
            _console = console;
            return this;
        }

        #endregion

        public Task ProduceAsync(TMessageModel message)
        {
            if (IsStopped())
            {
                throw new InvalidOperationException($"{_settings.GetPublisherName()}: publisher is not runned, can't produce the message");
            }

            lock (_items)
                _items.Enqueue(message);

            return Task.FromResult(0);
        }

        public RabbitMqPublisher<TMessageModel> Start()
        {
            if (_queueRepository == null)
            {
                throw new InvalidOperationException($"Please, setup queue repository, using {nameof(SetQueueRepository)}() method, before start publisher");
            }
            if (_serializer == null)
            {
                throw new InvalidOperationException($"Please, setup message serializer, using {nameof(SetSerializer)}() method, before start publisher");
            }

            if (_publishStrategy == null)
                _publishStrategy = new DefaultFanoutPublishStrategy(_settings);

            if (_thread == null)
            {
                _reconnectionsInARowCount = 0;

                LoadQueue();

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
            Stop();
        }

        public void Stop()
        {
            var thread = _thread;

            if (thread == null)
                return;

            if (_publishStrategy == null)
                _publishStrategy = new DefaultFanoutPublishStrategy(_settings);

            _thread = null;
            thread.Join();

            SaveQueue();
        }
        
        public void Dispose()
        {
            ((IStopable)this).Stop();
        }

        private void LoadQueue()
        {
            var items = _queueRepository.LoadAsync().Result;

            if (!items.Any())
            {
                return;
            }

            lock (_items)
            {
                if (items.Any())
                {
                    throw new InvalidOperationException($"{_settings.GetPublisherName()}: messages queue already not empty, can't load persisted messages");
                }

                foreach (var item in items)
                {
                    _items.Enqueue(item);
                }
            }
        }

        private void SaveQueue()
        {
            TMessageModel[] items;

            lock (_items)
            {
                items = _items.ToArray();
            }

            _queueRepository.SaveAsync(items).Wait();
        }

        private bool IsStopped()
        {
            return _thread == null;
        }

        private TMessageModel DequeueMessage()
        {
            lock (_items)
            {
                if (_items.Count > 0)
                    return _items.Dequeue();
            }

            return default(TMessageModel);
        }

        private void ConnectAndWrite()
        {
            var factory = new ConnectionFactory { Uri = _settings.ConnectionString };

            _console?.WriteLine($"{_settings.GetPublisherName()}: trying to connect to {_settings.ConnectionString} ({_settings.GetQueueOrExchangeName()})");

            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                _console?.WriteLine($"{_settings.GetPublisherName()}: connected to {_settings.ConnectionString} ({_settings.GetQueueOrExchangeName()})");
                _publishStrategy.Configure(_settings, channel);

                while (!IsStopped())
                {
                    if (!connection.IsOpen)
                    {
                        throw new RabbitMqBrokerException($"{_settings.GetPublisherName()}: connection to {_settings.ConnectionString} is closed");
                    }

                    var message = DequeueMessage();

                    if (message == null)
                    {
                        Thread.Sleep(20);
                        continue;
                    }

                    var body = _serializer.Serialize(message);
                    _publishStrategy.Publish(_settings, channel, body);

                    _reconnectionsInARowCount = 0;
                }
            }
        }

        private void ConnectionThread()
        {
            while (!IsStopped())
            {
                try
                {
                    try
                    {
                        ConnectAndWrite();
                    }
                    catch (Exception e)
                    {
                        _console?.WriteLine($"{_settings.GetPublisherName()}: ERROR: {e.Message}");

                        if (_reconnectionsInARowCount > _settings.ReconnectionsCountToAlarm)
                        {
                            _log?.WriteFatalErrorAsync(_settings.GetPublisherName(), nameof(ConnectionThread), "", e)
                                .Wait();

                            _reconnectionsInARowCount = 0;
                        }

                        _reconnectionsInARowCount++;

                        Thread.Sleep(_settings.ReconnectionDelay);
                    }
                }
                // Saves the loop if nothing didn't help
                // ReSharper disable once EmptyGeneralCatchClause
                catch
                {
                }
            }

            _console?.WriteLine($"{_settings.GetPublisherName()}: is stopped");
        }
    }
}
