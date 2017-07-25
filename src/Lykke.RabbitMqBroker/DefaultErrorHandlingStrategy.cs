using System;
using Common.Log;
using Lykke.RabbitMqBroker.Subscriber;

namespace Lykke.RabbitMqBroker
{
    public sealed class DefaultErrorHandlingStrategy : IErrorHandlingStrategy
    {
        private readonly ILog _log;
        private readonly RabbitMqSubscriptionSettings _settings;
        private readonly IErrorHandlingStrategy _next;

        public DefaultErrorHandlingStrategy(ILog log, RabbitMqSubscriptionSettings settings, IErrorHandlingStrategy next = null)
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
                _log.WriteErrorAsync(_settings.GetSubscriberName(), "Message handling", "", ex);
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