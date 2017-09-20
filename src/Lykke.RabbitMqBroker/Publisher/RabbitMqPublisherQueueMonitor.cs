using System;
using System.Threading.Tasks;
using Common;
using Common.Log;

namespace Lykke.RabbitMqBroker.Publisher
{
    internal class RabbitMqPublisherQueueMonitor<T> : TimerPeriod
    {
        private readonly string _publisherName;
        private readonly IPublisherBuffer<T> _queuePublisher;
        private readonly int _queueSizeThreshold;
        private readonly ILog _log;

        public RabbitMqPublisherQueueMonitor(string publisherName, IPublisherBuffer<T> queuePublisher, int queueSizeThreshold, TimeSpan checkPeriod, ILog log) :
            base(nameof(RabbitMqPublisherQueueMonitor<T>), (int)checkPeriod.TotalMilliseconds, log)
        {
            _publisherName = publisherName;
            _queuePublisher = queuePublisher;
            _queueSizeThreshold = queueSizeThreshold;
            _log = log;
        }

        public override async Task Execute()
        {
            var queueSize = _queuePublisher.Count;
            if (queueSize > _queueSizeThreshold)
            {
                await _log.WriteWarningAsync(nameof(RabbitMqPublisherQueueMonitor<T>), nameof(Execute), queueSize.ToString(),
                    $"{_publisherName} buffer size: {_queuePublisher.Count}. It exceeds threshold: {_queueSizeThreshold}");
            }
        }
    }
}