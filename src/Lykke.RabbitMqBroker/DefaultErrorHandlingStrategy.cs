using System;
using System.Threading;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Common.Log;
using Lykke.RabbitMqBroker.Subscriber;

namespace Lykke.RabbitMqBroker
{
    [PublicAPI]
    public sealed class DefaultErrorHandlingStrategy : IErrorHandlingStrategy
    {
        private readonly ILog _log;
        private readonly RabbitMqSubscriptionSettings _settings;
        private readonly IErrorHandlingStrategy _next;

        [Obsolete]
        public DefaultErrorHandlingStrategy(ILog log, RabbitMqSubscriptionSettings settings, IErrorHandlingStrategy next = null)
        {
            _log = log ?? throw new ArgumentNullException(nameof(log));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _next = next;
        }

        public DefaultErrorHandlingStrategy(
            [NotNull] ILogFactory logFactory, 
            [NotNull] RabbitMqSubscriptionSettings settings, 
            IErrorHandlingStrategy next = null)
        {
            if (logFactory == null)
            {
                throw new ArgumentNullException(nameof(logFactory));
            }

            _log = logFactory.CreateLog(this);
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _next = next;
        }

        public void Execute(Action handler, IMessageAcceptor ma, CancellationToken cancellationToken)
        {
            try
            {
                handler();
                ma.Accept();
            }
            catch (Exception ex)
            {
                // ReSharper disable once MethodSupportsCancellation
                _log.WriteErrorAsync(_settings.GetSubscriberName(), "Message handling", _settings.GetSubscriberName(), ex).GetAwaiter().GetResult();
                if (_next == null)
                {
                    ma.Accept();
                }
                else
                {
                    _next.Execute(handler, ma, cancellationToken);
                }
            }
        }
    }
}
