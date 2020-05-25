// Copyright (c) Lykke Corp.
// Licensed under the MIT License. See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace Lykke.RabbitMqBroker.Publisher
{
    internal sealed class RabbitMqPublisherQueueMonitor<T> : IStartStop
    {
        private readonly int _queueSizeThreshold;
        private readonly Timer _timer;
        private readonly TimeSpan _period;
        private readonly ILogger<RabbitMqPublisherQueueMonitor<T>> _logger;
        private IRawMessagePublisher _publisher;

        public RabbitMqPublisherQueueMonitor(
            int queueSizeThreshold,
            TimeSpan checkPeriod,
            ILogger<RabbitMqPublisherQueueMonitor<T>> logger)
        {
            if (queueSizeThreshold < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(queueSizeThreshold), queueSizeThreshold, "Should be positive number");
            }

            _queueSizeThreshold = queueSizeThreshold;
            _period = checkPeriod;
            _timer = new Timer(Execute, null, TimeSpan.FromMilliseconds(-1), checkPeriod);
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void WatchThis(IRawMessagePublisher publisher)
        {
            _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
        }

        public void Start()
        {
            if (_publisher == null)
            {
                throw new InvalidOperationException("Publisher is not set");
            }

            _timer.Change(TimeSpan.Zero, _period);
        }

        public void Stop()
        {
            _timer.Change(Timeout.Infinite, Timeout.Infinite);
        }

        public void Dispose()
        {
            Stop();

            _timer.Dispose();
        }

        private void Execute(object state)
        {
            var queueSize = _publisher.BufferedMessagesCount;
            if (queueSize > _queueSizeThreshold)
            {
                _logger.LogWarning($"{_publisher.Name} buffer size: {queueSize}. It exceeds threshold: {_queueSizeThreshold}");
            }
        }
    }
}
