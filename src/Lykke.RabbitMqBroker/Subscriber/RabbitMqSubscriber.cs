using System;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.PlatformAbstractions;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Autofac;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Common;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Common.Log;
using Lykke.RabbitMqBroker.Deduplication;
using Newtonsoft.Json;

namespace Lykke.RabbitMqBroker.Subscriber
{
    [PublicAPI]
    public class RabbitMqSubscriber<TTopicModel> : IStartable, IStopable, IMessageConsumer<TTopicModel>
    {
        private const string TelemetryType = "RabbitMq Subscriber";
        private Func<TTopicModel, Task> _eventHandler;
        private Func<TTopicModel, CancellationToken, Task> _cancallableEventHandler;
        private readonly RabbitMqSubscriptionSettings _settings;
        private readonly IErrorHandlingStrategy _errorHandlingStrategy;
        private readonly bool _submitTelemetry;
        private readonly TelemetryClient _telemetry = new TelemetryClient();
        private readonly string _exchangeQueueName;
        private readonly string _typeName = typeof(TTopicModel).Name;
        private readonly bool _useAlternativeExchange;
        private readonly bool _enableMessageDeduplication;
        private readonly IDeduplicator _deduplicator;
        private ILog _log;
        private Thread _thread;
        private Thread _alternateThread;
        private IMessageDeserializer<TTopicModel> _messageDeserializer;
        private IMessageReadStrategy _messageReadStrategy;
        private IConsole _console;
        private int _reconnectionsInARowCount;
        private CancellationTokenSource _cancellationTokenSource;
        private bool _disposed;

        [Obsolete]
        public RabbitMqSubscriber(
            RabbitMqSubscriptionSettings settings,
            IErrorHandlingStrategy errorHandlingStrategy,
            bool submitTelemetry = true,
            IDeduplicator deduplicator = null)
        {
            _settings = settings;
            _errorHandlingStrategy = errorHandlingStrategy;
            _submitTelemetry = submitTelemetry;
            _exchangeQueueName = _settings.GetQueueOrExchangeName();

            _useAlternativeExchange = !string.IsNullOrWhiteSpace(_settings.AlternativeConnectionString);
            _enableMessageDeduplication = _useAlternativeExchange;
            if (_enableMessageDeduplication)
                _deduplicator = deduplicator ?? new InMemoryDeduplcator();
        }

        public RabbitMqSubscriber(
            [NotNull] ILogFactory logFactory,
            [NotNull] RabbitMqSubscriptionSettings settings,
            [NotNull] IErrorHandlingStrategy errorHandlingStrategy,
            bool submitTelemetry = true,
            IDeduplicator deduplicator = null)
        {
            if (logFactory == null)
            {
                throw new ArgumentNullException(nameof(logFactory));
            }

            _log = logFactory.CreateLog(this);
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _errorHandlingStrategy = errorHandlingStrategy ?? throw new ArgumentNullException(nameof(errorHandlingStrategy));
            _submitTelemetry = submitTelemetry;
            _exchangeQueueName = _settings.GetQueueOrExchangeName();

            _useAlternativeExchange = !string.IsNullOrWhiteSpace(_settings.AlternativeConnectionString);
            _enableMessageDeduplication = _useAlternativeExchange;
            if (_enableMessageDeduplication)
                _deduplicator = deduplicator ?? new InMemoryDeduplcator();
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

        [Obsolete("Use ctor with ILogFactory")]
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
            return _cancellationTokenSource == null || _cancellationTokenSource.IsCancellationRequested;
        }

        private async void ReadThread(object parameter)
        {
            var settings = (RabbitMqSubscriptionSettings)parameter;
            while (!IsStopped())
            {
                try
                {
                    try
                    {
                        ConnectAndReadAsync(settings);
                    }
                    catch (Exception ex)
                    {
                        _console?.WriteLine($"{settings.GetSubscriberName()}: ERROR: {ex.Message}");

                        if (_reconnectionsInARowCount > settings.ReconnectionsCountToAlarm)
                        {
                            await _log.WriteFatalErrorAsync(settings.GetSubscriberName(), nameof(ReadThread), new Uri(settings.ConnectionString).Authority, ex);

                            _reconnectionsInARowCount = 0;
                        }

                        _reconnectionsInARowCount++;

                        await Task.Delay(settings.ReconnectionDelay, _cancellationTokenSource.Token);
                    }
                }
                // Saves the loop if nothing didn't help
                // ReSharper disable once EmptyGeneralCatchClause
                catch
                {
                }
            }

            _console?.WriteLine($"{settings.GetSubscriberName()}: is stopped");
        }

        private void ConnectAndReadAsync(RabbitMqSubscriptionSettings settings)
        {
            var factory = new ConnectionFactory { Uri = settings.ConnectionString };
            _console?.WriteLine($"{settings.GetSubscriberName()}: trying to connect to {settings.ConnectionString} ({_exchangeQueueName})");

            var cn = $"[Sub] {PlatformServices.Default.Application.ApplicationName} {PlatformServices.Default.Application.ApplicationVersion} to {_exchangeQueueName}";
            using (var connection = factory.CreateConnection(cn))
            using (var channel = connection.CreateModel())
            {
                _console?.WriteLine($"{settings.GetSubscriberName()}:  connected to {settings.ConnectionString} ({_exchangeQueueName})");

                var queueName = _messageReadStrategy.Configure(settings, channel);

                var consumer = new QueueingBasicConsumer(channel);
                var tag = channel.BasicConsume(queueName, false, consumer);

                //consumer.Received += MessageReceived;

                while (!IsStopped())
                {
                    if (!connection.IsOpen)
                    {
                        throw new RabbitMqBrokerException($"{settings.GetSubscriberName()}: connection to {connection.Endpoint.ToString()} is closed");
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
                    var isDuplicated = !_deduplicator.EnsureNotDuplicateAsync(body).GetAwaiter().GetResult();
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
                _console?.WriteLine("Failed to process the message");
                _log.WriteErrorAsync(GetType().Name, nameof(MessageReceived), "Failed to process the message", ex).GetAwaiter().GetResult();

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
            if (_log == null)
            {
                throw new InvalidOperationException("Please, specify log");
            }

            if (_cancellationTokenSource == null || _cancellationTokenSource.IsCancellationRequested)
            {
                _cancellationTokenSource = new CancellationTokenSource();
            }

            if (_messageReadStrategy == null)
                CreateDefaultBinding();

            if (_thread != null) return this;

            _reconnectionsInARowCount = 0;

            _thread = new Thread(ReadThread);
            _thread.Start(_settings);

            if (_useAlternativeExchange)
            {
                if (_alternateThread != null) return this;

                // deep clone
                var settings = JsonConvert.DeserializeObject<RabbitMqSubscriptionSettings>(JsonConvert.SerializeObject(_settings));
                // start a new thread which will use 'AlternativeConnectionString'
                settings.ConnectionString = settings.AlternativeConnectionString;
                _alternateThread = new Thread(ReadThread);
                _alternateThread.Start(settings);
            }

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

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed || !disposing)
                return;

            ((IStopable)this).Stop();

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
