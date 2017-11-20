using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Common;
using Common.Log;
using JetBrains.Annotations;
using Lykke.RabbitMqBroker.Publisher.DeferredMessages;
using Lykke.RabbitMqBroker.Subscriber;

namespace Lykke.RabbitMqBroker.Publisher
{
    [PublicAPI]
    public class RabbitMqPublisher<TMessageModel> :
        IMessageProducer<TMessageModel>,
        IStartable,
        IStopable
    {
        public string Name => _settings.GetPublisherName();

        // Dependencies

        private readonly RabbitMqSubscriptionSettings _settings;

        // Configuration

        private IPublishingQueueRepository _queueRepository;
        private bool _disableQueuePersistence;
        private IRabbitMqSerializer<TMessageModel> _serializer;
        private IRabbitMqPublishStrategy _publishStrategy;
        private ILog _log;
        private IConsole _console;
        private RabbitMqPublisherQueueMonitor<TMessageModel> _queueMonitor;
        private DeferredMessagesManager _deferredMessagesManager;
        private bool _publishSynchronously;

        // Implementation

        private IRawMessagePublisher _rawPublisher;

        // For testing

        private IPublisherBuffer _bufferOverriding;

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
            if (_rawPublisher != null)
            {
                throw new InvalidOperationException("Publisher already started");
            }

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
            if (_rawPublisher != null)
            {
                throw new InvalidOperationException("Publisher already started");
            }

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
            if (_rawPublisher != null)
            {
                throw new InvalidOperationException("Publisher already started");
            }
            if (queueSizeThreshold < 1)
            {
                throw new ArgumentException("Should be positive number", nameof(queueSizeThreshold));
            }
            if (_log == null)
            {
                throw new InvalidOperationException("Log should be set first");
            }

            _queueMonitor?.Dispose();

            _queueMonitor = new RabbitMqPublisherQueueMonitor<TMessageModel>(queueSizeThreshold, monitorPeriod ?? TimeSpan.FromSeconds(60), _log);

            return this;
        }

        /// <summary>
        /// Enables deferred messages publishing using <see cref="ProduceAsync(TMessageModel,System.TimeSpan)"/> 
        /// or <see cref="ProduceAsync(TMessageModel,System.DateTime)"/> methods
        /// </summary>
        /// <param name="repository">Deferred message repository instance</param>
        /// <param name="deliveryPrecision">
        /// The desired delivery time precision.
        /// Actually it determines the delay between deferred messages storage monitoring cycles.
        /// Default value is 1 second
        /// </param>
        /// <returns></returns>
        public RabbitMqPublisher<TMessageModel> EnableDeferredMessages(IDeferredMessagesRepository repository, TimeSpan? deliveryPrecision = null)
        {
            if (_rawPublisher != null)
            {
                throw new InvalidOperationException("Publisher already started");
            }
            if (_log == null)
            {
                throw new InvalidOperationException($"Log should be set before {nameof(EnableDeferredMessages)} call");
            }

            _deferredMessagesManager?.Dispose();

            _deferredMessagesManager = new DeferredMessagesManager(repository, deliveryPrecision ?? TimeSpan.FromSeconds(1), _log);

            return this;
        }

        public RabbitMqPublisher<TMessageModel> SetSerializer(IRabbitMqSerializer<TMessageModel> serializer)
        {
            if (_rawPublisher != null)
            {
                throw new InvalidOperationException("Publisher already started");
            }

            _serializer = serializer;
            return this;
        }
        
        /// <summary>
        /// Disables internal buffer. If exception occurred while publishing it will be re-thrown in the <see cref="ProduceAsync(TMessageModel)"/>
        /// </summary>
        public RabbitMqPublisher<TMessageModel> PublishSynchronously()
        {
            if (_rawPublisher != null)
            {
                throw new InvalidOperationException("Publisher already started");
            }

            DisableInMemoryQueuePersistence();
            _publishSynchronously = true;
            return this;
        }

        public RabbitMqPublisher<TMessageModel> SetPublishStrategy(IRabbitMqPublishStrategy publishStrategy)
        {
            if (_rawPublisher != null)
            {
                throw new InvalidOperationException("Publisher already started");
            }

            _publishStrategy = publishStrategy;
            return this;
        }

        public RabbitMqPublisher<TMessageModel> SetLogger(ILog log)
        {
            if (_rawPublisher != null)
            {
                throw new InvalidOperationException("Publisher already started");
            }

            _log = log;
            return this;
        }

        public RabbitMqPublisher<TMessageModel> SetConsole(IConsole console)
        {
            if (_rawPublisher != null)
            {
                throw new InvalidOperationException("Publisher already started");
            }

            _console = console;
            return this;
        }

        #endregion


        #region Publishing

        /// <summary>
        /// Publish the <paramref name="message"/> but defer the delivery for the <paramref name="delay"/>
        /// </summary>
        /// <remarks>
        /// The message will be deferred for at least <paramref name="delay"/>.
        /// Published message will be stored in the intermediate storage until the delay is elapsed.
        /// If the method is executed without exception, it is guaranteed that the message will be eventually delivered at least once
        /// </remarks>
        /// <param name="message">Message to publish</param>
        /// <param name="delay">The delay</param>
        public Task ProduceAsync(TMessageModel message, TimeSpan delay)
        {
            return ProduceAsync(message, DateTime.UtcNow + delay);
        }

        /// <summary>
        /// Publish the <paramref name="message"/> but deliver it later at the moment <paramref name="deliverAt"/>
        /// </summary>
        /// <remarks>
        /// The message will be delivered not early than <paramref name="deliverAt"/>.
        /// Published message will be stored in the intermediate storage until the delivery moment comes.
        /// If the method is executed without exception, it is guaranteed that the message will be eventually delivered at least once
        /// </remarks>
        /// <param name="message">Message to publish</param>
        /// <param name="deliverAt">Moment, when message should be delivered</param>
        /// <returns></returns>
        public Task ProduceAsync(TMessageModel message, DateTime deliverAt)
        {
            if (_rawPublisher == null)
            {
                throw new InvalidOperationException("Publisher isn't started");
            }
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }
            if (_deferredMessagesManager == null)
            {
                throw new InvalidOperationException($"To enable deferred messages publishing, call {nameof(EnableDeferredMessages)}");
            }

            var body = _serializer.Serialize(message);

            return _deferredMessagesManager.DeferAsync(body, deliverAt);
        }

        /// <summary>
        /// Publish <paramref name="message"/> to underlying queue
        /// </summary>
        /// <param name="message">Message to publish</param>
        /// <remarks>Method is not thread safe</remarks>
        /// <returns>Task to await</returns>
        /// <exception cref="RabbitMqBrokerException">Some error occurred while publishing</exception>
        public Task ProduceAsync(TMessageModel message)
        {
            if (_rawPublisher == null)
            {
                throw new InvalidOperationException("Publisher isn't started");
            }
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }
            
            var body = _serializer.Serialize(message);

            _rawPublisher.Produce(body);

            return Task.CompletedTask;
        }

        #endregion


        #region Start/stop

        public RabbitMqPublisher<TMessageModel> Start()
        {
            if (_rawPublisher != null)
            {
                return this;
            }

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

            var messagesBuffer = _bufferOverriding ?? LoadQueue();

            _rawPublisher = new RawMessagePublisher(
                Name,
                _log,
                _console,
                messagesBuffer,
                _publishStrategy,
                _settings,
                _publishSynchronously);
            
            _queueMonitor.WatchThis(_rawPublisher);
            _queueMonitor.Start();

            if (_deferredMessagesManager != null)
            {
                _deferredMessagesManager.Start();
                _deferredMessagesManager.PublishUsing(_rawPublisher);
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
            var rawPublisher = _rawPublisher;

            if (rawPublisher == null)
            {
                return;
            }

            _rawPublisher = null;
            
            try
            {
                _deferredMessagesManager?.Stop();
                _queueMonitor.Stop();

                rawPublisher.Stop();
            }
            finally
            {
                SaveQueue(rawPublisher.GetBufferedMessages());

                rawPublisher.Dispose();
            }
        }

        public void Dispose()
        {
            Stop();
        }

        #endregion


        #region Messages buffer persistence

        private IPublisherBuffer LoadQueue()
        {
            if (_disableQueuePersistence)
            {
                return new InMemoryBuffer();
            }

            var items = _queueRepository.LoadAsync(_settings.ExchangeName).Result;

            if (items == null || !items.Any())
            {
                return new InMemoryBuffer();
            }

            if (_rawPublisher != null)
            {
                throw new InvalidOperationException($"Publisher {Name} is already started, can't load persisted messages");
            }

            var buffer = new InMemoryBuffer();

            foreach (var item in items)
            {
                buffer.Enqueue(item, CancellationToken.None);
            }

            return buffer;
        }

        private void SaveQueue(IReadOnlyList<byte[]> bufferedMessages)
        {
            if (_disableQueuePersistence)
            {
                return;
            }
            
            _queueRepository.SaveAsync(bufferedMessages, _settings.ExchangeName).Wait();
        }

        #endregion


        #region For testing

        internal void SetBuffer(IPublisherBuffer buffer)
        {
            _bufferOverriding = buffer;
        }

        #endregion
    }
}
