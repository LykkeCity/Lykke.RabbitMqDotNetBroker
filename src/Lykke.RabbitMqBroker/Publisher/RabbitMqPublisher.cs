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
    public class RabbitMqPublisher<TMessageModel> : 
        IInMemoryQueuePublisher, 
        IMessageProducer<TMessageModel>, 
        IStartable, 
        IStopable
    {
        public int QueueSize
        {
            get
            {
                lock (_items)
                {
                    return _items.Count;
                }
            }
        }

        public string Name => _settings.GetPublisherName();
        
        private readonly RabbitMqSubscriptionSettings _settings;
        private IPublishingQueueRepository<TMessageModel> _queueRepository;
        private bool _disableQueuePersistence;
        private readonly Queue<TMessageModel> _items = new Queue<TMessageModel>();
        private Thread _thread;
        private IRabbitMqSerializer<TMessageModel> _serializer;
        private ILog _log;
        private IConsole _console;
        private IRabbitMqPublishStrategy _publishStrategy;
        private int _reconnectionsInARowCount;
        private RabbitMqPublisherQueueMonitor _queueMonitor;

        public RabbitMqPublisher(RabbitMqSubscriptionSettings settings)
        {
            _settings = settings;
        }

        #region Configurator

        /// <summary>
        /// Sets repository, which is used to save and load in-memory messages queue while starting and stopping 
        /// </summary>
        /// <remarks>
        /// Mutual exclusive with <see cref="DisableInMemoryQueuePersistence"/>, but one of which should be called
        /// </remarks>
        public RabbitMqPublisher<TMessageModel> SetQueueRepository(IPublishingQueueRepository<TMessageModel> queueRepository)
        {
            _queueRepository = queueRepository;

            return this;
        }

        /// <summary>
        /// Disables in-memory messages queue saving and loading while starting and stopping
        /// </summary>
        /// <remarks>
        /// Mutual exclusive with <see cref="SetQueueRepository"/>, but one of which should be called
        /// </remarks>
        public RabbitMqPublisher<TMessageModel> DisableInMemoryQueuePersistence()
        {
            _disableQueuePersistence = true;

            return this;
        }

        /// <summary>
        /// Configures in-memory messages queue size monitoring. Default monitor will be created, if you not call this method.
        /// Default monitor <paramref name="queueSizeThreshold"/> = 1000, <paramref name="monitorPeriod"/> = 10 seconds.
        /// </summary>
        /// <param name="queueSizeThreshold">Queue size threshold after which alarm will be enabled. Default is 1000</param>
        /// <param name="monitorPeriod">Queue size check period. Default is 10 seconds</param>
        public RabbitMqPublisher<TMessageModel> MonitorInMemoryQueue(int queueSizeThreshold = 1000, TimeSpan? monitorPeriod = null)
        {
            if (queueSizeThreshold < 1)
            {
                throw new ArgumentException("Should be positive number", nameof(queueSizeThreshold));
            }
            if (_log == null)
            {
                throw new InvalidOperationException("Log should be set first");
            }

            _queueMonitor = new RabbitMqPublisherQueueMonitor(this, queueSizeThreshold, monitorPeriod ?? TimeSpan.FromSeconds(10), _log);

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
                throw new InvalidOperationException($"{Name}: publisher is not runned, can't produce the message");
            }

            lock (_items)
                _items.Enqueue(message);

            return Task.FromResult(0);
        }

        public RabbitMqPublisher<TMessageModel> Start()
        {
            // Check configuration
            if (_queueRepository == null ^ _disableQueuePersistence)
            {
                throw new InvalidOperationException($"Please, do one of - setup queue repository, using {nameof(SetQueueRepository)}() method, or disable queue persistence using {nameof(DisableInMemoryQueuePersistence)}() method, before start publisher");
            }
            if (_serializer == null)
            {
                throw new InvalidOperationException($"Please, setup message serializer, using {nameof(SetSerializer)}() method, before start publisher");
            }
            if (_log == null)
            {
                throw new InvalidOperationException($"Please, setup logger, using {nameof(SetLogger)}() method, before start publisher");
            }

            // Set defaults
            if (_queueMonitor == null)
            {
                MonitorInMemoryQueue();
            }
            if (_publishStrategy == null)
            {
                SetPublishStrategy(new DefaultFanoutPublishStrategy(_settings));
            }

            if (_thread == null)
            {
                _reconnectionsInARowCount = 0;

                LoadQueue();

                _thread = new Thread(ConnectionThread);
                _thread.Start();

                _queueMonitor.Start();
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

            _queueMonitor.Stop();

            SaveQueue();
        }
        
        public void Dispose()
        {
            ((IStopable)this).Stop();
        }

        private void LoadQueue()
        {
            if (_disableQueuePersistence)
            {
                return;
            }

            var items = _queueRepository.LoadAsync(_settings.ExchangeName).Result;

            if (items == null || !items.Any())
            {
                return;
            }

            lock (_items)
            {
                if (_items.Any())
                {
                    throw new InvalidOperationException($"{Name}: messages queue already not empty, can't load persisted messages");
                }

                foreach (var item in items)
                {
                    _items.Enqueue(item);
                }
            }
        }

        private void SaveQueue()
        {
            if (_disableQueuePersistence)
            {
                return;
            }

            TMessageModel[] items;

            lock (_items)
            {
                items = _items.ToArray();
            }

            _queueRepository.SaveAsync(items, _settings.ExchangeName).Wait();
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

            _console?.WriteLine($"{Name}: trying to connect to {_settings.ConnectionString} ({_settings.GetQueueOrExchangeName()})");

            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                _console?.WriteLine($"{Name}: connected to {_settings.ConnectionString} ({_settings.GetQueueOrExchangeName()})");
                _publishStrategy.Configure(_settings, channel);

                while (!IsStopped())
                {
                    if (!connection.IsOpen)
                    {
                        throw new RabbitMqBrokerException($"{Name}: connection to {_settings.ConnectionString} is closed");
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
                        _console?.WriteLine($"{Name}: ERROR: {e.Message}");

                        if (_reconnectionsInARowCount > _settings.ReconnectionsCountToAlarm)
                        {
                            _log.WriteFatalErrorAsync(Name, nameof(ConnectionThread), "", e).Wait();

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

            _console?.WriteLine($"{Name}: is stopped");
        }
    }
}
