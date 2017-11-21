using System;
using System.Threading.Tasks;
using Common;
using Common.Log;

namespace Lykke.RabbitMqBroker.Publisher
{
    internal sealed class RabbitMqPublisherQueueMonitor<T> : TimerPeriod
    {
        private readonly int _queueSizeThreshold;
        private readonly ILog _log;
        private IRawMessagePublisher _publisher;

        public RabbitMqPublisherQueueMonitor(int queueSizeThreshold, TimeSpan checkPeriod, ILog log) :
            base(nameof(RabbitMqPublisherQueueMonitor<T>), (int)checkPeriod.TotalMilliseconds, log)
        {
            _queueSizeThreshold = queueSizeThreshold;
            _log = log;
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
