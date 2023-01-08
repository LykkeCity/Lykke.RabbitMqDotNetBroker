// Copyright (c) Lykke Corp.
// Licensed under the MIT License. See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Lykke.RabbitMqBroker.Logging;
using Lykke.RabbitMqBroker.Publisher.DeferredMessages;
using Lykke.RabbitMqBroker.Publisher.Serializers;
using Lykke.RabbitMqBroker.Publisher.Strategies;
using Microsoft.Extensions.Logging;

namespace Lykke.RabbitMqBroker.Publisher
{
    /// <summary>
    /// Generic rabbitMq publisher
    /// </summary>
    /// <typeparam name="TMessageModel"></typeparam>
    [PublicAPI]
    public class RabbitMqPublisher<TMessageModel> : IStartStop, IMessageProducer<TMessageModel>
    {
        private readonly RabbitMqSubscriptionSettings _settings;
        private readonly bool _submitTelemetry;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger<RabbitMqPublisher<TMessageModel>> _log;

        private IPublishingQueueRepository _queueRepository;
        private IRabbitMqSerializer<TMessageModel> _serializer;
        private IRabbitMqPublishStrategy _publishStrategy;
        private RabbitMqPublisherQueueMonitor<TMessageModel> _queueMonitor;
        private DeferredMessagesManager _deferredMessagesManager;
        private bool _disableQueuePersistence;
        private bool _publishSynchronously;
        private bool _disposed;
        private readonly List<Func<IDictionary<string, object>>> _writeHeadersFunсs = new List<Func<IDictionary<string, object>>>();

        private IRawMessagePublisher _rawPublisher;
        private IPublisherBuffer _bufferOverriding;
        private readonly OutgoingMessageBuilder _outgoingMessageBuilder;
        private readonly OutgoingMessageLogger _outgoingMessageLogger;

        public string Name => _settings.GetPublisherName();

        public int BufferedMessagesCount => _rawPublisher?.BufferedMessagesCount ?? 0;

        public RabbitMqPublisher(
            [NotNull] ILoggerFactory loggerFactory,
            [NotNull] RabbitMqSubscriptionSettings settings,
            bool submitTelemetry = true)
        {
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _submitTelemetry = submitTelemetry;

            _log = loggerFactory.CreateLogger<RabbitMqPublisher<TMessageModel>>();

            _outgoingMessageBuilder = new OutgoingMessageBuilder(_settings);
            _outgoingMessageLogger =
                new OutgoingMessageLogger(loggerFactory.CreateLogger<RabbitMqPublisher<TMessageModel>>());
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
            ThrowIfStarted();

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
            ThrowIfStarted();

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
            ThrowIfStarted();

            if (queueSizeThreshold < 1)
            {
                throw new ArgumentException("Should be positive number", nameof(queueSizeThreshold));
            }
            if (_log == null)
            {
                throw new InvalidOperationException("Log should be set first");
            }

            _queueMonitor?.Dispose();

            _queueMonitor = new RabbitMqPublisherQueueMonitor<TMessageModel>(
                queueSizeThreshold,
                monitorPeriod ?? TimeSpan.FromSeconds(60),
                _loggerFactory.CreateLogger<RabbitMqPublisherQueueMonitor<TMessageModel>>());

            return this;
        }

        /// <summary>
        /// Enables deferred messages publishing using <see cref="ProduceAsync(TMessageModel,System.TimeSpan, string)"/> 
        /// or <see cref="ProduceAsync(TMessageModel,System.DateTime, string)"/> methods
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
            ThrowIfStarted();

            if (_log == null)
            {
                throw new InvalidOperationException($"Log should be set before {nameof(EnableDeferredMessages)} call");
            }

            _deferredMessagesManager?.Dispose();

            _deferredMessagesManager = new DeferredMessagesManager(
                repository,
                deliveryPrecision ?? TimeSpan.FromSeconds(1),
                _loggerFactory.CreateLogger<DeferredMessagesManager>());

            return this;
        }

        public RabbitMqPublisher<TMessageModel> SetSerializer(IRabbitMqSerializer<TMessageModel> serializer)
        {
            ThrowIfStarted();

            _serializer = serializer;
            _outgoingMessageBuilder.SetSerializationFormat(_serializer.SerializationFormat);
            return this;
        }

        /// <summary>
        /// Disables internal buffer. If exception occurred while publishing it will be re-thrown in the <see cref="ProduceAsync(TMessageModel, string)"/>
        /// </summary>
        public RabbitMqPublisher<TMessageModel> PublishSynchronously()
        {
            ThrowIfStarted();

            DisableInMemoryQueuePersistence();
            _publishSynchronously = true;
            return this;
        }

        public RabbitMqPublisher<TMessageModel> SetPublishStrategy(IRabbitMqPublishStrategy publishStrategy)
        {
            ThrowIfStarted();

            _publishStrategy = publishStrategy;
            return this;
        }
        
        public RabbitMqPublisher<TMessageModel> SetWriteHeadersFunc(Func<IDictionary<string, object>> func)
        {
            if (func != null)
            {
                _writeHeadersFunсs.Add(func);
            }
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
        /// <param name="routingKey">Message routing key. Overrides routing key, which specified in the publisher settings</param>
        public Task ProduceAsync(TMessageModel message, TimeSpan delay, string routingKey = null)
        {
            return ProduceAsync(message, DateTime.UtcNow + delay, routingKey);
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
        /// <param name="routingKey">Message routing key. Overrides routing key, which specified in the publisher settings</param>
        /// <returns></returns>
        public Task ProduceAsync(TMessageModel message, DateTime deliverAt, string routingKey = null)
        {
            ThrowIfNotStarted();

            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }
            if (_deferredMessagesManager == null)
            {
                throw new InvalidOperationException($"To enable deferred messages publishing, call {nameof(EnableDeferredMessages)}");
            }

            var body = _serializer.Serialize(message);
            var headers = GetMessageHeaders();

            var loggedMessage = _outgoingMessageBuilder.Create<TMessageModel>(body, headers);
            _outgoingMessageLogger.Log(loggedMessage);

            return _deferredMessagesManager.DeferAsync(new RawMessage(body, routingKey, headers), deliverAt);
        }

        /// <summary>
        /// Publish <paramref name="message"/> to underlying queue
        /// </summary>
        /// <param name="message">Message to publish</param>
        /// <param name="routingKey">Message routing key. Overrides routing key, which specified in the publisher settings</param>
        /// <remarks>Method is not thread safe</remarks>
        /// <returns>Task to await</returns>
        /// <exception cref="RabbitMqBrokerException">Some error occurred while publishing</exception>
        public Task ProduceAsync(TMessageModel message, string routingKey)
        {
            ThrowIfNotStarted();

            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            var body = _serializer.Serialize(message);
            var headers = GetMessageHeaders();
            var loggedMessage = _outgoingMessageBuilder.Create<TMessageModel>(body, headers);
            _outgoingMessageLogger.Log(loggedMessage);

            _rawPublisher.Produce(new RawMessage(body, routingKey, headers));

            return Task.CompletedTask;
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
            return ProduceAsync(message, routingKey: null);
        }

        #endregion


        #region Start/stop

        public void Start()
        {
            if (_rawPublisher != null)
                return;

            // Check configuration

            if (_queueRepository == null ^ _disableQueuePersistence)
            {
                throw new InvalidOperationException($"Please, do one of - setup queue repository, using {nameof(SetQueueRepository)}() method, or disable queue persistence using {nameof(DisableInMemoryQueuePersistence)}() method, before start publisher");
            }
            if (_serializer == null)
            {
                throw new InvalidOperationException($"Please, setup message serializer, using {nameof(SetSerializer)}() method, before start publisher");
            }

            // Set defaults

            if (_queueMonitor == null)
            {
                MonitorInMemoryQueue();
            }
            if (_publishStrategy == null)
            {
                SetPublishStrategy(new FanoutPublishStrategy(_settings));
            }

            var messagesBuffer = _bufferOverriding ?? LoadQueue();

            _rawPublisher = new RawMessagePublisher(
                Name,
                _loggerFactory.CreateLogger<RawMessagePublisher>(),
                messagesBuffer,
                _publishStrategy,
                _settings,
                _publishSynchronously,
                _submitTelemetry);

            _queueMonitor.WatchThis(_rawPublisher);
            _queueMonitor.Start();

            if (_deferredMessagesManager != null)
            {
                _deferredMessagesManager.Start();
                _deferredMessagesManager.PublishUsing(_rawPublisher);
            }
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
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed || !disposing)
                return; 

            Stop();

            _disposed = true;
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

            ThrowIfStarted();

            var buffer = new InMemoryBuffer();

            foreach (var item in items)
            {
                buffer.Enqueue(item, CancellationToken.None);
            }

            return buffer;
        }

        private void SaveQueue(IReadOnlyList<RawMessage> bufferedMessages)
        {
            if (_disableQueuePersistence)
            {
                return;
            }
            
            _queueRepository.SaveAsync(bufferedMessages, _settings.ExchangeName).GetAwaiter().GetResult();
        }

        #endregion


        #region For testing

        internal void SetBuffer(IPublisherBuffer buffer)
        {
            _bufferOverriding = buffer;
        }

        #endregion


        #region Private stuff
        
        private IDictionary<string, object> GetMessageHeaders()
        {
            var result = new Dictionary<string, object>();
            
            var keyValuePairs = _writeHeadersFunсs
                .Select(x => x())
                .Where(x => x != null && x.Any())
                .SelectMany(x => x);

            if (keyValuePairs.Any())
            {
                foreach (var keyValuePair in keyValuePairs)
                {
                    if (result.ContainsKey(keyValuePair.Key))
                    {
                        _log.LogError($"Header with key '{keyValuePair.Key}' already exists. Discarded value is '${keyValuePair.Value}'. Please, use unique headers only.");
                    }
                    else
                    {
                        result.Add(keyValuePair.Key, keyValuePair.Value);
                    }
                }
            }
            
            return result;
        }


        private void ThrowIfStarted()
        {
            if (_rawPublisher != null)
            {
                throw new InvalidOperationException($"Publisher {Name} is already started");
            }
        }

        private void ThrowIfNotStarted()
        {
            if (_rawPublisher == null)
            {
                throw new InvalidOperationException($"Publisher {Name} isn't started yet");
            }
        }

        #endregion
    }
}
