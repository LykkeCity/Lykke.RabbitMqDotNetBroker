// Copyright (c) Lykke Corp.
// Licensed under the MIT License. See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace Lykke.RabbitMqBroker.Subscriber.Strategies
{
    [PublicAPI]
    public sealed class DeadQueueErrorHandlingStrategy : IErrorHandlingStrategy
    {
        private readonly ILogger<DeadQueueErrorHandlingStrategy> _logger;
        private readonly RabbitMqSubscriptionSettings _settings;

        public DeadQueueErrorHandlingStrategy(
            [NotNull] ILogger<DeadQueueErrorHandlingStrategy> logger,
            [NotNull] RabbitMqSubscriptionSettings settings)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
                _logger.LogWarning(
                    $"Failed to handle the message. Send it to poison queue {_settings.GetSubscriberName()} {_settings.QueueName}-poison. Exception {ex}");
                ma.Reject();
            }
        }
    }
}
