// Copyright (c) Lykke Corp.
// Licensed under the MIT License. See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Common;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Common.Log;

namespace Lykke.RabbitMqBroker.Publisher
{
    internal sealed class RabbitMqPublisherQueueMonitor<T> : TimerPeriod
    {
        private readonly int _queueSizeThreshold;
        private readonly ILog _log;
        private IRawMessagePublisher _publisher;

        [Obsolete]
        public RabbitMqPublisherQueueMonitor(int queueSizeThreshold, TimeSpan checkPeriod, ILog log) :
            base(nameof(RabbitMqPublisherQueueMonitor<T>), (int)checkPeriod.TotalMilliseconds, log)
        {
            _queueSizeThreshold = queueSizeThreshold;
            _log = log;
        }

        public RabbitMqPublisherQueueMonitor(
            int queueSizeThreshold, 
            TimeSpan checkPeriod, 
            [NotNull] ILogFactory logFactory) :

            base(checkPeriod, logFactory)
        {
            if (logFactory == null)
            {
                throw new ArgumentNullException(nameof(logFactory));
            }
            if (queueSizeThreshold < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(queueSizeThreshold), queueSizeThreshold, "Should be positive number");
            }

            _queueSizeThreshold = queueSizeThreshold;

            _log = logFactory.CreateLog(this);
        }

        public void WatchThis(IRawMessagePublisher publisher)
        {
            _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
        }

        public override void Start()
        {
            if (_publisher == null)
            {
                throw new InvalidOperationException("Publisher is not set");
            }

            base.Start();
        }

        public override async Task Execute()
        {
            var queueSize = _publisher.BufferedMessagesCount;
            if (queueSize > _queueSizeThreshold)
            {
                await _log.WriteWarningAsync(nameof(RabbitMqPublisherQueueMonitor<T>), nameof(Execute), queueSize.ToString(), $"{_publisher.Name} buffer size: {queueSize}. It exceeds threshold: {_queueSizeThreshold}");
            }
        }
    }
}
