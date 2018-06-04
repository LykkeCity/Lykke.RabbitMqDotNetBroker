using System;
using System.Threading;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Common.Log;
using Lykke.RabbitMqBroker.Subscriber;

namespace Lykke.RabbitMqBroker
{
    [PublicAPI]
    public sealed class DeadQueueErrorHandlingStrategy : IErrorHandlingStrategy
    {
        private readonly ILog _log;
        private readonly RabbitMqSubscriptionSettings _settings;

        [Obsolete]
        public DeadQueueErrorHandlingStrategy(ILog log, RabbitMqSubscriptionSettings settings)
        {
            _log = log;
            _settings = settings;
        }

        public DeadQueueErrorHandlingStrategy(
            [NotNull] ILogFactory logFactory,
            [NotNull] RabbitMqSubscriptionSettings settings)
        {
            if (logFactory == null)
            {
                throw new ArgumentNullException(nameof(logFactory));
            }

            _log = logFactory.CreateLog(this);
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
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
                _log.WriteWarningAsync(
                        nameof(ResilientErrorHandlingStrategy),
                        _settings.GetSubscriberName(),
                        "Message handling",
                        $"Failed to handle the message. Send it to poison queue {_settings.QueueName}-poison. Exception {ex}")
                    .GetAwaiter().GetResult();
                ma.Reject();
            }
        }
    }
}
