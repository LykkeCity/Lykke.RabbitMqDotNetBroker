using System;
using System.Threading;
using System.Threading.Tasks;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Common.Log;
using Lykke.RabbitMqBroker.Subscriber;

namespace Lykke.RabbitMqBroker
{
    [PublicAPI]
    public sealed class ResilientErrorHandlingStrategy : IErrorHandlingStrategy
    {
        private readonly ILog _log;
        private readonly RabbitMqSubscriptionSettings _settings;
        private readonly TimeSpan _retryTimeout;
        private readonly int _retryNum;
        private readonly IErrorHandlingStrategy _next;

        [Obsolete]
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

        public ResilientErrorHandlingStrategy(
            [NotNull] ILogFactory logFactory, 
            [NotNull] RabbitMqSubscriptionSettings settings, 
            TimeSpan retryTimeout, 
            int retryNum = 5, 
            IErrorHandlingStrategy next = null)
        {
            if (logFactory == null)
            {
                throw new ArgumentNullException(nameof(logFactory));
            }
            if (retryTimeout <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(retryTimeout), retryTimeout, "Should be positive time span");
            }
            if (retryNum <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(retryNum), retryNum, "Should be positive number");
            }

            _log = logFactory.CreateLog(this);
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _retryTimeout = retryTimeout;
            _retryNum = retryNum;
            _next = next;
        }

        public void Execute(Action handler, IMessageAcceptor ma, CancellationToken cancellationToken)
        {
            var isAllAttemptsFailed = false;

            try
            {
                handler();
            }
            catch (Exception ex)
            {
                // Message processing is failed, entering to the retries

                // ReSharper disable once MethodSupportsCancellation
                _log.WriteWarningAsync(
                        nameof(ResilientErrorHandlingStrategy),
                        _settings.GetSubscriberName(),
                        "Message handling",
                        $"Failed to handle the message for the first time. Retry in {_retryTimeout.Seconds} sec. Exception {ex}")
                    .GetAwaiter().GetResult();

                // Retries loop

                for (int i = 0; i < _retryNum; i++)
                {
                    // Adding delay between attempts

                    // ReSharper disable once MethodSupportsCancellation
                    Task.Delay(_retryTimeout, cancellationToken).GetAwaiter().GetResult();

                    try
                    {
                        handler();

                        // The message was processed after all, so no more retries needed

                        return;
                    }
                    catch (Exception ex2)
                    {
                        // ReSharper disable once MethodSupportsCancellation
                        _log.WriteWarningAsync(
                                nameof(ResilientErrorHandlingStrategy),
                                _settings.GetSubscriberName(),
                                "Message handling",
                                $"Failed to handle the message for the {i + 1} time. Retry in {_retryTimeout.Seconds} sec. Exception {ex2}")
                            .GetAwaiter().GetResult();
                    }
                }

                // All attempts is failed, need to call next strategy in the chain

                isAllAttemptsFailed = true;

                if (_next == null)
                {
                    // Swallow the message if no more strategy is chained

                    ma.Accept();
                }
                else
                {
                    _next.Execute(handler, ma, cancellationToken);
                }
            }
            finally
            {
                // Finally, the message should be accepted, if it was successfully processed

                if (!isAllAttemptsFailed)
                {
                    ma.Accept();
                }
            }
        }
    }
}
