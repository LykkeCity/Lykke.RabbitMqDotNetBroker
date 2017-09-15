using System;
using System.Threading.Tasks;
using Common;
using Common.Log;

namespace Lykke.RabbitMqBroker.Publisher
{
    internal class RabbitMqPublisherQueueMonitor : TimerPeriod
    {
        private readonly IInMemoryQueuePublisher _queuePublisher;
        private readonly int _queueSizeThreshold;
        private readonly ILog _log;

        public RabbitMqPublisherQueueMonitor(IInMemoryQueuePublisher queuePublisher, int queueSizeThreshold, TimeSpan checkPeriod, ILog log) : 
            base(nameof(RabbitMqPublisherQueueMonitor), (int)checkPeriod.TotalMilliseconds, log)
        {
            _queuePublisher = queuePublisher;
            _queueSizeThreshold = queueSizeThreshold;
            _log = log;
        }

        public override async Task Execute()
        {
            var queueSize = _queuePublisher.QueueSize;
            if (queueSize > _queueSizeThreshold)
            {
                await _log.WriteWarningAsync(nameof(RabbitMqPublisherQueueMonitor), nameof(Execute), queueSize.ToString(),
                    $"{_queuePublisher.Name} in-memory queue has grown to much");
            }
        }
    }
}