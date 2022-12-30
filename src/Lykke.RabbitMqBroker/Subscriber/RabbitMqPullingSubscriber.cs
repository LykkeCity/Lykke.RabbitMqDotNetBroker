// Copyright (c) Lykke Corp.
// Licensed under the MIT License. See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using Autofac;
using JetBrains.Annotations;
using Lykke.RabbitMqBroker.Subscriber.Deserializers;
using Lykke.RabbitMqBroker.Subscriber.MessageReadStrategies;
using Lykke.RabbitMqBroker.Subscriber.Middleware;
using Lykke.RabbitMqBroker.Subscriber.Middleware.Deduplication;
using Microsoft.Extensions.PlatformAbstractions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;

namespace Lykke.RabbitMqBroker.Subscriber
{
    /// <summary>
    /// Generic rabbitMq subscriber
    /// </summary>
    [PublicAPI]
    public class RabbitMqPullingSubscriber<TTopicModel> : IStartStop
    {
        private readonly RabbitMqSubscriptionSettings _settings;
        private readonly string _exchangeQueueName;

        private Func<TTopicModel, Task> _eventHandler;
        private Func<TTopicModel, CancellationToken, Task> _cancellableEventHandler;
        private bool _useAlternativeExchange;
        private bool _disposed;
        private string _alternativeExchangeConnString;
        private int _reconnectionsInARowCount;
        private ushort? _prefetchCount;
        private ILogger<RabbitMqPullingSubscriber<TTopicModel>> _logger;
        private Thread _thread;
        private Thread _alternateThread;
        private IMessageDeserializer<TTopicModel> _messageDeserializer;
        private IMessageReadStrategy _messageReadStrategy;
        private CancellationTokenSource _cancellationTokenSource;
        private MiddlewareQueue<TTopicModel> _middlewareQueue;
        private readonly List<Action<IDictionary<string, object>>> _readHeadersActions = new List<Action<IDictionary<string, object>>>();


        public RabbitMqPullingSubscriber(
            [NotNull] ILogger<RabbitMqPullingSubscriber<TTopicModel>> logger,
            [NotNull] RabbitMqSubscriptionSettings settings)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _exchangeQueueName = _settings.GetQueueOrExchangeName();
            _middlewareQueue = new MiddlewareQueue<TTopicModel>(settings);
        }

        #region Configurator

        public RabbitMqPullingSubscriber<TTopicModel> SetMessageDeserializer(
            IMessageDeserializer<TTopicModel> messageDeserializer)
        {
            _messageDeserializer = messageDeserializer;
            return this;
        }

        public RabbitMqPullingSubscriber<TTopicModel> Subscribe(Func<TTopicModel, Task> callback)
        {
            _eventHandler = callback;
            _cancellableEventHandler = null;
            return this;
        }

        public RabbitMqPullingSubscriber<TTopicModel> Subscribe(Func<TTopicModel, CancellationToken, Task> callback)
        {
            _cancellableEventHandler = callback;
            _eventHandler = null;
            return this;
        }

        public RabbitMqPullingSubscriber<TTopicModel> SetMessageReadStrategy(IMessageReadStrategy messageReadStrategy)
        {
            _messageReadStrategy = messageReadStrategy;
            return this;
        }

        public RabbitMqPullingSubscriber<TTopicModel> CreateDefaultBinding()
        {
            SetMessageReadStrategy(new MessageReadWithTemporaryQueueStrategy());
            return this;
        }

        public RabbitMqPullingSubscriber<TTopicModel> SetAlternativeExchange(string connString)
        {
            if (!string.IsNullOrWhiteSpace(connString))
            {
                _alternativeExchangeConnString = connString;
                _useAlternativeExchange = true;
            }

            return this;
        }

        public RabbitMqPullingSubscriber<TTopicModel> UseMiddleware(IEventMiddleware<TTopicModel> middleware)
        {
            if (!IsStopped())
                throw new InvalidOperationException("New middleware can't be added after subscriber Start");

            _middlewareQueue.AddMiddleware(middleware ?? throw new ArgumentNullException());
            return this;
        }

        public RabbitMqPullingSubscriber<TTopicModel> SetPrefetchCount(ushort prefetchCount)
        {
            _prefetchCount = prefetchCount;
            return this;
        }
        
        public RabbitMqPullingSubscriber<TTopicModel> SetReadHeadersAction(Action<IDictionary<string, object>> action)
        {
            if (action != null)
            {
                _readHeadersActions.Add(action);
            }
            return this;
        }


        #endregion

        private bool IsStopped()
        {
            return _cancellationTokenSource == null || _cancellationTokenSource.IsCancellationRequested;
        }

        private void ReadThread(object parameter)
        {
            var settings = (RabbitMqSubscriptionSettings)parameter;
            while (!IsStopped())
            {
                try
                {
                    try
                    {
                        ConnectAndRead(settings);
                    }
                    catch (Exception ex)
                    {
                        if (_reconnectionsInARowCount > settings.ReconnectionsCountToAlarm)
                        {
                            _logger.LogError(ex, $"{new Uri(settings.ConnectionString).Authority} - {settings.GetSubscriberName()}");

                            _reconnectionsInARowCount = 0;
                        }

                        _reconnectionsInARowCount++;

                        Thread.Sleep(settings.ReconnectionDelay);
                    }
                }
                // Saves the loop if nothing didn't help
                // ReSharper disable once EmptyGeneralCatchClause
                catch
                {
                }
            }

            _logger.LogInformation($"Subscriber {settings.GetSubscriberName()} is stopped");
        }

        private void ConnectAndRead(RabbitMqSubscriptionSettings settings)
        {
            var factory = new ConnectionFactory {Uri = new Uri(settings.ConnectionString, UriKind.Absolute)};
            _logger.LogInformation($"{settings.GetSubscriberName()}: Trying to connect to {factory.Endpoint} ({_exchangeQueueName})");

            var cn = $"[Sub] {PlatformServices.Default.Application.ApplicationName} {PlatformServices.Default.Application.ApplicationVersion} to {_exchangeQueueName}";
            
            using (var connection = factory.CreateConnection(cn))
            using (var channel = connection.CreateModel())
            {
                _logger.LogInformation($"{settings.GetSubscriberName()}: Connected to {factory.Endpoint} ({_exchangeQueueName})");

                if (_prefetchCount.HasValue)
                    channel.BasicQos(0, _prefetchCount.Value, false);

                var queueName = _messageReadStrategy.Configure(settings, channel);

                var consumer = new DefaultBasicConsumer(channel);
                var tag = channel.BasicConsume(queueName, false, consumer);

                while (!IsStopped())
                {
                    if (!connection.IsOpen)
                    {
                        throw new RabbitMqBrokerException($"{settings.GetSubscriberName()}: connection to {connection.Endpoint} is closed");
                    }
                    
                    var result = channel.BasicGet(queueName, false);

                    _reconnectionsInARowCount = 0;

                    if (result != null)
                    {
                        MessageReceived(channel, result);
                    }
                }
                
                channel.BasicCancel(tag);
            }
        }

        private void MessageReceived(IModel channel, BasicGetResult result)
        {
            var ma = new MessageAcceptor(channel, result.DeliveryTag);

            _readHeadersActions.ForEach(x => x(result.BasicProperties?.Headers));
            
            try
            {
                var model = _messageDeserializer.Deserialize(result.Body.ToArray());

                try
                {
                    _middlewareQueue.RunMiddlewaresAsync(
                            result.Body,
                            result.BasicProperties,
                            model,
                            ma,
                            _cancellationTokenSource.Token)
                        .GetAwaiter().GetResult();
                }
                catch (OperationCanceledException ex)
                {
                    _logger.LogError(ex, "Middleware queue processing was cancelled");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, _settings.GetSubscriberName());

                ma.Reject();
            }
        }

        void IStartable.Start()
        {
            Start();
        }

        public RabbitMqPullingSubscriber<TTopicModel> Start()
        {
            if (_messageDeserializer == null)
                throw new InvalidOperationException("Please, specify message deserializer");

            if (_eventHandler == null && _cancellableEventHandler == null)
                throw new InvalidOperationException("Please, specify message handler");

            if (_useAlternativeExchange && !_middlewareQueue.HasMiddleware<InMemoryDeduplicationMiddleware<TTopicModel>>())
                throw new InvalidOperationException("Please, specify deduplicator");

            if (_cancellationTokenSource == null || _cancellationTokenSource.IsCancellationRequested)
            {
                _cancellationTokenSource = new CancellationTokenSource();
            }

            if (_messageReadStrategy == null)
                CreateDefaultBinding();

            if (_thread != null)
                return this;

            _reconnectionsInARowCount = 0;

            var actualHandlerMiddleware = _eventHandler != null
                ? new ActualHandlerMiddleware<TTopicModel>(_eventHandler)
                : new ActualHandlerMiddleware<TTopicModel>(_cancellableEventHandler);
            _middlewareQueue.AddMiddleware(actualHandlerMiddleware);

            _thread = new Thread(ReadThread);
            _thread.Start(_settings);

            if (_useAlternativeExchange)
            {
                if (_alternateThread != null)
                    return this;

                // deep clone
                var settings = JsonConvert.DeserializeObject<RabbitMqSubscriptionSettings>(JsonConvert.SerializeObject(_settings));
                // start a new thread which will use 'AlternativeConnectionString'
                settings.ConnectionString = _alternativeExchangeConnString;
                _alternateThread = new Thread(ReadThread);
                _alternateThread.Start(settings);
            }

            return this;
        }

        public void Stop()
        {
            var thread = _thread;

            if (thread == null)
                return;

            _thread = null;

            Thread alternateThread = null;
            if (_useAlternativeExchange)
            {
                alternateThread = _alternateThread;
                _alternateThread = null;
            }

            _cancellationTokenSource?.Cancel();

            thread.Join();

            if (_useAlternativeExchange)
            {
                alternateThread?.Join();
            }

            _cancellationTokenSource?.Dispose();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (_disposed || !disposing)
                return;

            Stop();

            _disposed = true;
        }
    }
}
