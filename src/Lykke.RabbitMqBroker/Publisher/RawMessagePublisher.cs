// Copyright (c) Lykke Corp.
// Licensed under the MIT License. See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.PlatformAbstractions;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Common.Log;
using Lykke.RabbitMqBroker.Subscriber;
using RabbitMQ.Client;

namespace Lykke.RabbitMqBroker.Publisher
{
    internal sealed class RawMessagePublisher : IRawMessagePublisher
    {
        public string Name { get; }
        public int BufferedMessagesCount => _buffer.Count;

        private const string TelemetryType = "RabbitMq Publisher";

        private readonly ILog _log;
        private readonly IPublisherBuffer _buffer;
        private readonly IRabbitMqPublishStrategy _publishStrategy;
        private readonly RabbitMqSubscriptionSettings _settings;
        private readonly bool _publishSynchronously;
        private readonly bool _submitTelemetry;
        private readonly AutoResetEvent _publishLock;
        private readonly Thread _thread;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly TelemetryClient _telemetry = new TelemetryClient();
        private readonly string _exchangeQueueName;
        private bool _disposed;

        private Exception _lastPublishException;
        private int _reconnectionsInARowCount;

        [Obsolete]
        public RawMessagePublisher(
            string name,
            ILog log,
            IPublisherBuffer buffer,
            IRabbitMqPublishStrategy publishStrategy,
            RabbitMqSubscriptionSettings settings,
            bool publishSynchronously,
            bool submitTelemetry)
        {
            Name = name;
            _log = log;
            _buffer = buffer;
            _settings = settings;
            _publishSynchronously = publishSynchronously;
            _publishStrategy = publishStrategy;
            _submitTelemetry = submitTelemetry;
            _exchangeQueueName = _settings.GetQueueOrExchangeName();

            _publishLock = new AutoResetEvent(false);
            _cancellationTokenSource = new CancellationTokenSource();

            _thread = new Thread(ConnectionThread)
            {
                Name = "RabbitMqPublisherLoop"
            };

            _thread.Start();
        }

        public RawMessagePublisher(
            [NotNull] string name,
            [NotNull] ILogFactory logFactory, 
            [NotNull] IPublisherBuffer buffer,
            [NotNull] IRabbitMqPublishStrategy publishStrategy,
            [NotNull] RabbitMqSubscriptionSettings settings,
            bool publishSynchronously,
            bool submitTelemetry)
        {
            if (logFactory == null)
            {
                throw new ArgumentNullException(nameof(logFactory));
            }

            Name = name ?? throw new ArgumentNullException(nameof(name));
            _buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _publishSynchronously = publishSynchronously;
            _publishStrategy = publishStrategy ?? throw new ArgumentNullException(nameof(publishStrategy));
            _submitTelemetry = submitTelemetry;
            _exchangeQueueName = _settings.GetQueueOrExchangeName();

            _log = logFactory.CreateLog(this, Name);

            _publishLock = new AutoResetEvent(false);
            _cancellationTokenSource = new CancellationTokenSource();
            
            _thread = new Thread(ConnectionThread)
            {
                Name = "RabbitMqPublisherLoop"
            };

            _thread.Start();
        }

        public void Produce(RawMessage message)
        {
            if (IsStopped())
            {
                throw new InvalidOperationException($"{Name}: publisher is not run, can't produce the message");
            }

            _buffer.Enqueue(message, _cancellationTokenSource.Token);

            if (_publishSynchronously)
            {
                _publishLock.WaitOne();
                if (_lastPublishException != null)
                {
                    var tmp = _lastPublishException;
                    _lastPublishException = null;
                    while (_buffer.Count > 0) // An exception occurred before we get a message from the queue. Drop it.
                    {
                        _buffer.Dequeue(CancellationToken.None);
                    }
                    throw new RabbitMqBrokerException("Unable to publish message. See inner exception for details", tmp);
                }
            }
        }

        public IReadOnlyList<RawMessage> GetBufferedMessages()
        {
            if (!IsStopped())
            {
                throw new InvalidOperationException("Buffered messages can be obtained only if the publisher is stopped");
            }

            return _buffer.ToArray();
        }

        public void Stop()
        {
            if (IsStopped())
            {
                return;
            }

            _cancellationTokenSource?.Cancel();

            if (_publishSynchronously)
            {
                _publishLock.Set();
            }

            _thread.Join();
        }

        public void Dispose()
        {
            Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            if (_disposed || !disposing)
                return; 
            
            Stop();
            
            _publishLock?.Dispose();
            _buffer?.Dispose();
            _cancellationTokenSource?.Dispose();
            
            _disposed = true;
        }
        
        private bool IsStopped()
        {
            return _cancellationTokenSource.IsCancellationRequested;
        }

        private void ConnectAndWrite()
        {
            var factory = new ConnectionFactory {Uri = new Uri(_settings.ConnectionString, UriKind.Absolute)};

            _log.WriteInfo(nameof(ConnectAndWrite), _settings.GetPublisherName(), $"{Name}: trying to connect to {factory.Endpoint} ({_exchangeQueueName})");

            var cn = $"[Pub] {PlatformServices.Default.Application.ApplicationName} {PlatformServices.Default.Application.ApplicationVersion} to {_settings.ExchangeName ?? ""}";
            using (var connection = factory.CreateConnection(cn))
            using (var channel = connection.CreateModel())
            {
                _log.WriteInfo(nameof(ConnectAndWrite), _settings.GetPublisherName(), $"{Name}: connected to {factory.Endpoint} ({_exchangeQueueName})");
                _publishStrategy.Configure(_settings, channel);

                while (!IsStopped())
                {
                    RawMessage message;
                    try
                    {
                        message = _buffer.WaitOneAndPeek(_cancellationTokenSource.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        return;
                    }

                    if (message == null)
                    {
                        continue;
                    }

                    if (!connection.IsOpen)
                    {
                        throw new RabbitMqBrokerException($"{Name}: connection to {connection.Endpoint} is closed");
                    }

                    if (_submitTelemetry)
                    {
                        var telemetryOperation = InitTelemetryOperation(message);
                        try
                        {
                            _publishStrategy.Publish(_settings, channel, message);
                        }
                        catch (Exception e)
                        {
                            telemetryOperation.Telemetry.Success = false;
                            _telemetry.TrackException(e);
                            throw;
                        }
                        finally
                        {
                            _telemetry.StopOperation(telemetryOperation);
                        }
                    }
                    else
                    {
                        _publishStrategy.Publish(_settings, channel, message);
                    }

                    _buffer.Dequeue(_cancellationTokenSource.Token);
                    
                    if (_publishSynchronously)
                        _publishLock.Set();

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
                        _lastPublishException = e;
                        if (_publishSynchronously)
                            _publishLock.Set();

                        if (_reconnectionsInARowCount > _settings.ReconnectionsCountToAlarm)
                        {
                            _log.WriteFatalError(Name, _settings.GetPublisherName(), e);

                            _reconnectionsInARowCount = 0;
                        }

                        _reconnectionsInARowCount++;

                        Thread.Sleep(_settings.ReconnectionDelay);
                    }
                }
                // ReSharper disable once EmptyGeneralCatchClause
                // Saves the loop if nothing didn't help
                catch
                {
                }
            }

            _log.WriteInfo(nameof(ConnectionThread), _settings.GetPublisherName(), $"{Name}: is stopped");
        }

        private IOperationHolder<DependencyTelemetry> InitTelemetryOperation(RawMessage message)
        {
            var effectiveRoutingKey = message.RoutingKey ?? _settings.RoutingKey;
            var operation = _telemetry.StartOperation<DependencyTelemetry>(_exchangeQueueName);
            operation.Telemetry.Type = TelemetryType;
            operation.Telemetry.Target = effectiveRoutingKey != null ? $"{_exchangeQueueName}:{effectiveRoutingKey}" : _exchangeQueueName;
            operation.Telemetry.Name = Name;
            operation.Telemetry.Data = $"Binary length {message.Body.Length}";

            return operation;
        }
    }
}
