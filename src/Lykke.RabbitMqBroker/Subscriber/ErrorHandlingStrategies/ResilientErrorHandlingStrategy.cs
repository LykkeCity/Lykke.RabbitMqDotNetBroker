// Copyright (c) Lykke Corp.
// Licensed under the MIT License. See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace Lykke.RabbitMqBroker.Subscriber.ErrorHandlingStrategies
{
    [PublicAPI]
    public sealed class ResilientErrorHandlingStrategy : IErrorHandlingStrategy
    {
        private readonly ILogger<ResilientErrorHandlingStrategy> _logger;
        private readonly RabbitMqSubscriptionSettings _settings;
        private readonly TimeSpan _retryTimeout;
        private readonly int _retryNum;
        private readonly IErrorHandlingStrategy _next;

        public ResilientErrorHandlingStrategy(
            [NotNull] ILogger<ResilientErrorHandlingStrategy> logger,
            [NotNull] RabbitMqSubscriptionSettings settings,
            TimeSpan retryTimeout,
            int retryNum = 5,
            IErrorHandlingStrategy next = null)
        {
            if (retryTimeout <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(retryTimeout), retryTimeout, "Should be positive time span");
            }
            if (retryNum <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(retryNum), retryNum, "Should be positive number");
            }

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
                _logger.LogWarning(
                    $"Failed to handle the message from {_settings.GetSubscriberName()} for the first time. Retry in {_retryTimeout.Seconds} sec. Exception {ex}");

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
                        _logger.LogWarning(
                            $"Failed to handle the message from {_settings.GetSubscriberName()} for the {i + 1} time. Retry in {_retryTimeout.Seconds} sec. Exception {ex2}");
                    }
                }

                // All attempts is failed, need to call next strategy in the chain

                isAllAttemptsFailed = true;

                if (_next == null)
                {
                    // Reject the message if no more strategy is chained

                    ma.Reject();
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
