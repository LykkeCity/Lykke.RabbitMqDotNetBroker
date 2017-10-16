using System;
using System.Threading.Tasks;
using Common;
using Common.Log;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Threading;
using Autofac;

namespace Lykke.RabbitMqBroker.Subscriber
{
    public interface IMessageDeserializer<out TModel>
    {
        TModel Deserialize(byte[] data);
    }

    public interface IMessageReadStrategy
    {
        string Configure(RabbitMqSubscriptionSettings settings, IModel channel);
    }

    public class RabbitMqSubscriber<TTopicModel> : IStartable, IStopable, IMessageConsumer<TTopicModel>
    {
        private Func<TTopicModel, Task> _eventHandler;
        private Func<TTopicModel, CancellationToken, Task> _cancallableEventHandler;
        private readonly RabbitMqSubscriptionSettings _settings;
        private readonly IErrorHandlingStrategy _errorHandlingStrategy;
        private ILog _log;
        private Thread _thread;
        private IMessageDeserializer<TTopicModel> _messageDeserializer;
        private IMessageReadStrategy _messageReadStrategy;
        private IConsole _console;
        private int _reconnectionsInARowCount;
        private CancellationTokenSource _cancellationTokenSource;

        public RabbitMqSubscriber(RabbitMqSubscriptionSettings settings, IErrorHandlingStrategy errorHandlingStrategy)
        {
            _settings = settings;
            _errorHandlingStrategy = errorHandlingStrategy;
        }

        [Obsolete("Use RabbitMqSubscriber(RabbitMqSubscriptionSettings settings, IErrorHandlingStrategy errorHandlingStrategy) and settings.ReconnectionDelay to specify reconnection delay")]
        public RabbitMqSubscriber(RabbitMqSubscriptionSettings settings, IErrorHandlingStrategy errorHandlingStrategy, int reconnectTimeOut = 3000)
        {
            _settings = settings;
            _errorHandlingStrategy = errorHandlingStrategy;
            _settings.ReconnectionDelay = TimeSpan.FromMilliseconds(reconnectTimeOut);
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

        private async void ReadThread()
        {
            while (!IsStopped())
            {
                try
                {
                    try
                    {
                        ConnectAndReadAsync();
                    }
                    catch (Exception ex)
                    {
                        _console?.WriteLine($"{_settings.GetSubscriberName()}: ERROR: {ex.Message}");

                        if (_reconnectionsInARowCount > _settings.ReconnectionsCountToAlarm)
                        {
                            _log.WriteFatalErrorAsync(_settings.GetSubscriberName(), nameof(ReadThread), "", ex).Wait();

                            _reconnectionsInARowCount = 0;
                        }

                        _reconnectionsInARowCount++;

                        await Task.Delay(_settings.ReconnectionDelay, _cancellationTokenSource.Token);
                    }
                }
                // Saves the loop if nothing didn't help
                // ReSharper disable once EmptyGeneralCatchClause
                catch
                {
                }
            }

            _console?.WriteLine($"{_settings.GetSubscriberName()}: is stopped");
        }

        private void ConnectAndReadAsync()
        {
            var factory = new ConnectionFactory { Uri = _settings.ConnectionString };
            _console?.WriteLine($"{_settings.GetSubscriberName()}: trying to connect to {_settings.ConnectionString} ({_settings.GetQueueOrExchangeName()})");

            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                _console?.WriteLine($"{_settings.GetSubscriberName()}:  connected to {_settings.ConnectionString} ({_settings.GetQueueOrExchangeName()})");

                var queueName = _messageReadStrategy.Configure(_settings, channel);

                var consumer = new QueueingBasicConsumer(channel);
                var tag = channel.BasicConsume(queueName, false, consumer);

                //consumer.Received += MessageReceived;

                while (!IsStopped())
                {
                    if (!connection.IsOpen)
                    {
                        throw new RabbitMqBrokerException($"{_settings.GetSubscriberName()}: connection to {_settings.ConnectionString} is closed");
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
            var body = basicDeliverEventArgs.Body;
            var model = _messageDeserializer.Deserialize(body);

            var ma = new MessageAcceptor(channel, tag);

            try
            {
                if (_cancallableEventHandler != null)
                {
                    _errorHandlingStrategy.Execute(
                        () => _cancallableEventHandler(model, _cancellationTokenSource.Token).Wait(),
                        ma,
                        _cancellationTokenSource.Token);
                }
                else
                {
                    _errorHandlingStrategy.Execute(() => _eventHandler(model).Wait(), ma,
                        _cancellationTokenSource.Token);
                }
            }
            catch (OperationCanceledException)
            {
            }
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
            _thread.Start();
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

            _cancellationTokenSource?.Cancel();

            thread.Join();

            _cancellationTokenSource?.Dispose();
        }

        public void Dispose()
        {
            ((IStopable) this).Stop();
        }
    }
}
