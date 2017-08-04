using System;
using System.Threading;
using Common.Log;
using Lykke.RabbitMqBroker.Subscriber;

namespace Lykke.RabbitMqBroker
{
    public sealed class ResilientErrorHandlingStrategy : IErrorHandlingStrategy
    {
        private readonly ILog _log;
        private readonly RabbitMqSubscriptionSettings _settings;
        private readonly TimeSpan _retryTimeout;
        private readonly int _retryNum;
        private readonly IErrorHandlingStrategy _next;

        public ResilientErrorHandlingStrategy(ILog log, RabbitMqSubscriptionSettings settings, TimeSpan retryTimeout, int retryNum = 5, IErrorHandlingStrategy next = null)
        {
            if (log == null)
            {
                throw new ArgumentNullException(nameof(log));
            }
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            _log = log;
            _settings = settings;
            _retryTimeout = retryTimeout;
            _retryNum = retryNum;
            _next = next;
        }
        public void Execute(Action handler, IMessageAcceptor ma)
        {
            try
            {
                handler();
                ma.Accept();
            }
            catch (Exception ex)
            {
                _log.WriteWarningAsync(nameof(ResilientErrorHandlingStrategy), _settings.GetSubscriberName(), "Message handling", $"Failed to handle the message for the first time. Retry in {_retryTimeout.Seconds} sec. Exception {ex}");

                for (int i = 0; i < _retryNum; i++)
                {
                    Thread.Sleep(_retryTimeout);
                    try
                    {
                        handler();
                        ma.Accept();
                    }
                    catch (Exception ex2)
                    {
                        _log.WriteWarningAsync(nameof(ResilientErrorHandlingStrategy), _settings.GetSubscriberName(), "Message handling", $"Failed to handle the message for the {i + 1} time. Retry in {_retryTimeout.Seconds} sec. Exception {ex2}");
                    }
                }
                if (_next == null)
                {
                    ma.Accept();
                }
                else
                {
                    _next.Execute(handler, ma);
                }
            }
        }
    }
}