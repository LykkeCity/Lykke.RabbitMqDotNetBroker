// Copyright (c) Lykke Corp.
// Licensed under the MIT License. See the LICENSE file in the project root for more information.

using System;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Autofac;
using JetBrains.Annotations;
using Lykke.RabbitMqBroker.Deduplication;
using Lykke.RabbitMqBroker.Subscriber.Deserializers;
using Lykke.RabbitMqBroker.Subscriber.ErrorHandlingStrategies;
using Lykke.RabbitMqBroker.Subscriber.MessageReadStrategies;
using Microsoft.Extensions.PlatformAbstractions;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Lykke.RabbitMqBroker.Subscriber
{
    /// <summary>
    /// Generic rabbitMq subscriber
    /// </summary>
    [PublicAPI]
    public class RabbitMqSubscriber<TTopicModel> : IStartStop
    {
        private const string TelemetryType = "RabbitMq Subscriber";

        private readonly RabbitMqSubscriptionSettings _settings;
        private readonly IErrorHandlingStrategy _errorHandlingStrategy;
        private readonly TelemetryClient _telemetry = new TelemetryClient();
        private readonly bool _submitTelemetry;
        private readonly string _exchangeQueueName;
        private readonly string _typeName = typeof(TTopicModel).Name;

        private Func<TTopicModel, Task> _eventHandler;
        private Func<TTopicModel, CancellationToken, Task> _cancallableEventHandler;
        private bool _enableMessageDeduplication;
        private bool _useAlternativeExchange;
        private bool _disposed;
        private string _alternativeExchangeConnString;
        private string _deduplicatorHeader;
        private int _reconnectionsInARowCount;
        private ushort? _prefetchCount;
        private IDeduplicator _deduplicator;
        private ILogger<RabbitMqSubscriber<TTopicModel>> _logger;
        private Thread _thread;
        private Thread _alternateThread;
        private IMessageDeserializer<TTopicModel> _messageDeserializer;
        private IMessageReadStrategy _messageReadStrategy;
        private CancellationTokenSource _cancellationTokenSource;

        public RabbitMqSubscriber(
            [NotNull] ILogger<RabbitMqSubscriber<TTopicModel>> logger,
            [NotNull] RabbitMqSubscriptionSettings settings,
            [NotNull] IErrorHandlingStrategy errorHandlingStrategy,
            bool submitTelemetry = true,
            IDeduplicator deduplicator = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _errorHandlingStrategy = errorHandlingStrategy ?? throw new ArgumentNullException(nameof(errorHandlingStrategy));
            _submitTelemetry = submitTelemetry;
            _exchangeQueueName = _settings.GetQueueOrExchangeName();
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
            _cancallableEventHandler = null;
            return this;
        }

        public RabbitMqSubscriber<TTopicModel> Subscribe(Func<TTopicModel, CancellationToken, Task> callback)
        {
            _cancallableEventHandler = callback;
            _eventHandler = null;
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

        public RabbitMqSubscriber<TTopicModel> SetAlternativeExchange(string connString)
        {
            if (!string.IsNullOrWhiteSpace(connString))
            {
                _alternativeExchangeConnString = connString;
                _useAlternativeExchange = true;
            }

            return this;
        }

        public RabbitMqSubscriber<TTopicModel> SetDeduplicator(IDeduplicator deduplicator)
        {
            _deduplicator = deduplicator;
            return this;
        }

        public RabbitMqSubscriber<TTopicModel> SetHeaderDeduplication(string headerName)
        {
            _deduplicatorHeader = headerName;
            return this;
        }

        public RabbitMqSubscriber<TTopicModel> SetPrefetchCount(ushort prefetchCount)
        {
            _prefetchCount = prefetchCount;
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
            _logger.LogInformation($"Trying to connect to {factory.Endpoint} ({settings.GetSubscriberName()} - {_exchangeQueueName})");

            var cn = $"[Sub] {PlatformServices.Default.Application.ApplicationName} {PlatformServices.Default.Application.ApplicationVersion} to {_exchangeQueueName}";
            using (var connection = factory.CreateConnection(cn))
            using (var channel = connection.CreateModel())
            {
                _logger.LogInformation($"Connected to {factory.Endpoint} ({settings.GetSubscriberName()} - {_exchangeQueueName})");

                if (_prefetchCount.HasValue)
                    channel.BasicQos(0, _prefetchCount.Value, false);

                var queueName = _messageReadStrategy.Configure(settings, channel);

                var consumer = new QueueingBasicConsumer(channel);
                var tag = channel.BasicConsume(queueName, false, consumer);

                //consumer.Received += MessageReceived;

                while (!IsStopped())
                {
                    if (!connection.IsOpen)
                    {
                        throw new RabbitMqBrokerException($"{settings.GetSubscriberName()}: connection to {connection.Endpoint} is closed");
                    }

                    var delivered = consumer.Queue.Dequeue(2000, out var eventArgs);

                    _reconnectionsInARowCount = 0;

                    if (delivered)
                    {
                        MessageReceived(eventArgs, channel);
                    }
                }

                channel.BasicCancel(tag);
                connection.Close();
            }
        }

        private void MessageReceived(BasicDeliverEventArgs basicDeliverEventArgs, IModel channel)
        {
            var tag = basicDeliverEventArgs.DeliveryTag;
            var ma = new MessageAcceptor(channel, tag);

            try
            {
                var body = basicDeliverEventArgs.Body;

                if (_enableMessageDeduplication)
                {
                    var deduplicationHeaderBytes = string.IsNullOrEmpty(_deduplicatorHeader) ||
                                 !basicDeliverEventArgs.BasicProperties.Headers.ContainsKey(_deduplicatorHeader)
                        ? Array.Empty<byte>()
                        : Encoding.UTF8.GetBytes(
                            JsonConvert.SerializeObject(
                                basicDeliverEventArgs.BasicProperties.Headers[_deduplicatorHeader],
                                new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
                    var isDuplicated = deduplicationHeaderBytes.Length == 0
                        ? !_deduplicator.EnsureNotDuplicateAsync(body).GetAwaiter().GetResult()
                        : !_deduplicator.EnsureNotDuplicateAsync(deduplicationHeaderBytes).GetAwaiter().GetResult();

                    if (isDuplicated)
                    {
                        ma.Accept();
                        return;
                    }
                }

                var model = _messageDeserializer.Deserialize(body);

                if (_submitTelemetry)
                {
                    var telemetryOperation = InitTelemetryOperation(body.Length);
                    try
                    {
                        ExecuteStrategy(model, ma);
                    }
                    catch (Exception e)
                    {
                        telemetryOperation.Telemetry.Success = false;
                        _telemetry.TrackException(e);
                        if (!(e is OperationCanceledException))
                            throw;
                    }
                    finally
                    {
                        _telemetry.StopOperation(telemetryOperation);
                    }
                }
                else
                {
                    try
                    {
                        ExecuteStrategy(model, ma);
                    }
                    catch (OperationCanceledException)
                    {
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, _settings.GetSubscriberName());

                ma.Reject();
            }
        }

        private void ExecuteStrategy(TTopicModel model, MessageAcceptor ma)
        {
            _errorHandlingStrategy.Execute(
                _cancallableEventHandler != null
                ? () => _cancallableEventHandler(model, _cancellationTokenSource.Token).GetAwaiter().GetResult()
                : (Action)(() => _eventHandler(model).GetAwaiter().GetResult()),
                ma,
                _cancellationTokenSource.Token);
        }

        void IStartable.Start()
        {
            Start();
        }

        public RabbitMqSubscriber<TTopicModel> Start()
        {
            if (_messageDeserializer == null)
            {
                throw new InvalidOperationException("Please, specify message deserializer");
            }

            if (_eventHandler == null && _cancallableEventHandler == null)
            {
                throw new InvalidOperationException("Please, specify message handler");
            }

            if (_deduplicator == null && _useAlternativeExchange)
            {
                throw new InvalidOperationException("Please, specify deduplicator");
            }

            if (_cancellationTokenSource == null || _cancellationTokenSource.IsCancellationRequested)
            {
                _cancellationTokenSource = new CancellationTokenSource();
            }

            if (_messageReadStrategy == null)
                CreateDefaultBinding();

            _enableMessageDeduplication = _useAlternativeExchange;

            if (_thread != null)
                return this;

            _reconnectionsInARowCount = 0;

            _thread = new Thread(ReadThread);
            _thread.Start(_settings);

            if (_useAlternativeExchange)
            {
                if (_alternateThread != null) return this;

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

        private IOperationHolder<DependencyTelemetry> InitTelemetryOperation(int binaryLength)
        {
            var operation = _telemetry.StartOperation<DependencyTelemetry>(_exchangeQueueName);
            operation.Telemetry.Type = TelemetryType;
            operation.Telemetry.Target = _exchangeQueueName;
            operation.Telemetry.Name = _typeName;
            operation.Telemetry.Data = $"Binary length {binaryLength}";

            return operation;
        }
    }
}
