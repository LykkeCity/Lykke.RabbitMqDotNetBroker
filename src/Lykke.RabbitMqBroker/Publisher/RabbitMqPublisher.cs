using System;
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
        IMessageProducer<TMessageModel>,
        IStartable,
        IStopable
    {
        public string Name => _settings.GetPublisherName();

        private readonly RabbitMqSubscriptionSettings _settings;

        private readonly AutoResetEvent _publishLock = new AutoResetEvent(false);

        private IPublishingQueueRepository _queueRepository;
        private bool _disableQueuePersistence;

        private Thread _thread;
        private IRabbitMqSerializer<TMessageModel> _serializer;
        private ILog _log;
        private IConsole _console;
        private IRabbitMqPublishStrategy _publishStrategy;
        private int _reconnectionsInARowCount;
        private RabbitMqPublisherQueueMonitor<TMessageModel> _queueMonitor;
        private IPublisherBuffer _items;
        private CancellationTokenSource _cancellationTokenSource;

        private bool _publishSynchronously;

        private Exception _lastPublishException;


        public RabbitMqPublisher(RabbitMqSubscriptionSettings settings)
        {

            _settings = settings;
        }

        #region Configurator

        /// <summary>
        /// Sets repository, which is used to save and load in-memory messages queue while starting and stopping 
        /// </summary>
        /// <param name="queueRepository"> The queue Repository. </param>
        /// <remarks>
        /// Mutual exclusive with <see cref="DisableInMemoryQueuePersistence"/>, but one of which should be called
        /// </remarks>
        /// <returns>
        /// The <see cref="T:RabbitMqPublisher"/>.
        /// </returns>
        public RabbitMqPublisher<TMessageModel> SetQueueRepository(IPublishingQueueRepository queueRepository)
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

            _queueMonitor = new RabbitMqPublisherQueueMonitor<TMessageModel>(Name, _items, queueSizeThreshold, monitorPeriod ?? TimeSpan.FromSeconds(10), _log);

            return this;
        }

        public RabbitMqPublisher<TMessageModel> SetSerializer(IRabbitMqSerializer<TMessageModel> serializer)
        {
            _serializer = serializer;
            return this;
        }


        /// <summary>
        /// Disables internal buffer. If exception occurred while publishing it will be re-thrown in the <see cref="ProduceAsync"/>
        /// </summary>
        public RabbitMqPublisher<TMessageModel> PublishSynchronously()
        {
            DisableInMemoryQueuePersistence();
            _publishSynchronously = true;
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

        /// <summary>
        /// Publish <paramref name="message"/> to underlying queue
        /// </summary>
        /// <param name="message">Message to publish</param>
        /// <remarks>Method is not thread safe</remarks>
        /// <returns>Task to await</returns>
        /// <exception cref="RabbitMqBrokerException">Some error occurred while publishing</exception>
        public Task ProduceAsync(TMessageModel message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }
            if (IsStopped())
            {
                throw new InvalidOperationException($"{Name}: publisher is not run, can't produce the message");
            }

            var body = _serializer.Serialize(message);

            _items.Enqueue(body, _cancellationTokenSource.Token);

            if (_publishSynchronously)
            {
                _publishLock.WaitOne();
                if (_lastPublishException != null)
                {
                    var tmp = _lastPublishException;
                    _lastPublishException = null;
                    throw new RabbitMqBrokerException("Unable to publish message. See inner exception for details", tmp);
                }
            }

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

            if (_cancellationTokenSource == null)
            {
                _cancellationTokenSource = new CancellationTokenSource();
            }
            else
            {
                if (_cancellationTokenSource.IsCancellationRequested)
                {
                    _cancellationTokenSource = new CancellationTokenSource();
                }
            }

            if (_items == null)
            {
                _items = new InMemoryBuffer();
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

                _thread = new Thread(ConnectionThread) { Name = "RabbitMqPublisherLoop" };
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
            {
                return;
            }

            try
            {
                if (_publishStrategy == null)
                {
                    _publishStrategy = new DefaultFanoutPublishStrategy(_settings);
                }

                _thread = null;
                if (_cancellationTokenSource != null)
                {
                    _cancellationTokenSource.Cancel();
                    _cancellationTokenSource.Dispose();
                }
                _publishLock.Set();
                thread.Join();
                _queueMonitor.Stop();
            }
            finally
            {
                SaveQueue();
            }
        }

        public void Dispose()
        {
            Stop();
            _items?.Dispose();
        }

        internal RabbitMqPublisher<TMessageModel> SetBuffer(IPublisherBuffer buffer)
        {
            _items = buffer;
            return this;
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

            if (_items.Count > 0)
            {
                throw new InvalidOperationException($"{Name}: messages queue is already not empty, can't load persisted messages");
            }

            foreach (var item in items)
            {
                _items.Enqueue(item, CancellationToken.None);
            }
        }

        private void SaveQueue()
        {
            if (_disableQueuePersistence)
            {
                return;
            }


            var items = _items.ToArray();

            _queueRepository.SaveAsync(items, _settings.ExchangeName).Wait();
        }

        private bool IsStopped()
        {
            return _cancellationTokenSource == null || _cancellationTokenSource.IsCancellationRequested;
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
                    byte[] message;
                    try
                    {
                        message = _items.Dequeue(_cancellationTokenSource.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        return;
                    }

                    if (!connection.IsOpen)
                    {
                        throw new RabbitMqBrokerException($"{Name}: connection to {_settings.ConnectionString} is closed");
                    }

              
                    _publishStrategy.Publish(_settings, channel, message);
                    _publishLock.Set();

                    _reconnectionsInARowCount = 0;
                }
            }
        }

        private async void ConnectionThread()
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
                        _lastPublishException = e;
                        _publishLock.Set();


                        _console?.WriteLine($"{Name}: ERROR: {e.Message}");

                        if (_reconnectionsInARowCount > _settings.ReconnectionsCountToAlarm)
                        {
                            await _log.WriteFatalErrorAsync(Name, nameof(ConnectionThread), string.Empty, e);

                            _reconnectionsInARowCount = 0;
                        }

                        _reconnectionsInARowCount++;

                        await Task.Delay(_settings.ReconnectionDelay, _cancellationTokenSource.Token);
                    }
                } // ReSharper disable once EmptyGeneralCatchClause
                // Saves the loop if nothing didn't help
                catch
                {
                }
            }

            _console?.WriteLine($"{Name}: is stopped");
        }
    }
}
