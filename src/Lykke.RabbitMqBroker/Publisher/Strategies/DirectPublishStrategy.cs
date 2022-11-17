// Copyright (c) Lykke Corp.
// Licensed under the MIT License. See the LICENSE file in the project root for more information.

namespace Lykke.RabbitMqBroker.Publisher.Strategies
{
    /// <summary>
    /// Publish strategy for direct exchange.
    /// </summary>
    public sealed class DirectPublishStrategy : BasicPublishStrategy
    {
        public DirectPublishStrategy(RabbitMqSubscriptionSettings settings)
            : base(settings, RabbitMQ.Client.ExchangeType.Direct)
        {
        }
    }
}
