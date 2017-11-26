using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common.Log;
using Lykke.RabbitMqBroker.Subscriber;
using Microsoft.Extensions.PlatformAbstractions;
using RabbitMQ.Client;

namespace Lykke.RabbitMqBroker.Publisher
{
    internal class RawMessagePublisher : IRawMessagePublisher
    {
        public string Name { get; }
        public int BufferedMessagesCount => _buffer.Count;

        private readonly ILog _log;
        private readonly IConsole _console;
        private readonly IPublisherBuffer _buffer;
        private readonly IRabbitMqPublishStrategy _publishStrategy;
        private readonly RabbitMqSubscriptionSettings _settings;
        private readonly bool _publishSynchronously;

        private readonly AutoResetEvent _publishLock;
        private readonly Thread _thread;
        private readonly CancellationTokenSource _cancellationTokenSource;
        
        private Exception _lastPublishException;
        private int _reconnectionsInARowCount;

        public RawMessagePublisher(
            string name,
            ILog log, 
            IConsole console,
            IPublisherBuffer buffer,
            IRabbitMqPublishStrategy publishStrategy,
            RabbitMqSubscriptionSettings settings,
            bool publishSynchronously)
        {
            Name = name;
            _log = log;
            _console = console;
            _buffer = buffer;
            _settings = settings;
            _publishSynchronously = publishSynchronously;
            _publishStrategy = publishStrategy;

            _publishLock = new AutoResetEvent(false);
            _cancellationTokenSource = new CancellationTokenSource();
            
            _thread = new Thread(ConnectionThread)
            {
                Name = "RabbitMqPublisherLoop"
            };

            _thread.Start();
        }

        public void Produce(byte[] body)
        {
            if (IsStopped())
            {
                throw new InvalidOperationException($"{Name}: publisher is not run, can't produce the message");
            }

            _buffer.Enqueue(body, _cancellationTokenSource.Token);

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

        public IReadOnlyList<byte[]> GetBufferedMessages()
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
            Stop();

            _publishLock?.Dispose();
            _buffer?.Dispose();
            _cancellationTokenSource?.Dispose();
        }
        
        private bool IsStopped()
        {
            return _cancellationTokenSource.IsCancellationRequested;
        }

        private void ConnectAndWrite()
        {
            var factory = new ConnectionFactory { Uri = _settings.ConnectionString };

            _console?.WriteLine($"{Name}: trying to connect to {_settings.ConnectionString} ({_settings.GetQueueOrExchangeName()})");

            var cn = $"[Pub] {PlatformServices.Default.Application.ApplicationName} {PlatformServices.Default.Application.ApplicationVersion}";
            using (var connection = factory.CreateConnection(cn))
            using (var channel = connection.CreateModel())
            {
                _console?.WriteLine($"{Name}: connected to {_settings.ConnectionString} ({_settings.GetQueueOrExchangeName()})");
                _publishStrategy.Configure(_settings, channel);

                while (!IsStopped())
                {
                    byte[] message;
                    try
                    {
                        message = _buffer.Dequeue(_cancellationTokenSource.Token);
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
                    if (_publishSynchronously)
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
                        if (_publishSynchronously)
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
                } 
                // ReSharper disable once EmptyGeneralCatchClause
                // Saves the loop if nothing didn't help
                catch
                {
                }
            }

            _console?.WriteLine($"{Name}: is stopped");
        }
    }
}
