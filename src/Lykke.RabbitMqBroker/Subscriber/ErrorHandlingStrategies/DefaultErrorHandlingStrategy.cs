// Copyright (c) Lykke Corp.
// Licensed under the MIT License. See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace Lykke.RabbitMqBroker.Subscriber.ErrorHandlingStrategies
{
    [PublicAPI]
    public sealed class DefaultErrorHandlingStrategy : IErrorHandlingStrategy
    {
        private readonly ILogger<DefaultErrorHandlingStrategy> _logger;
        private readonly RabbitMqSubscriptionSettings _settings;
        private readonly IErrorHandlingStrategy _next;

        public DefaultErrorHandlingStrategy(
            [NotNull] ILogger<DefaultErrorHandlingStrategy> logger,
            [NotNull] RabbitMqSubscriptionSettings settings,
            IErrorHandlingStrategy next = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
                _logger.LogError(ex, _settings.GetSubscriberName());
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
