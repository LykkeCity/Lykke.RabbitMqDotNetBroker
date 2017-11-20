using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.Log;

namespace Lykke.RabbitMqBroker.Publisher.DeferredMessages
{
    internal class DeferredMessagesManager : TimerPeriod
    {
        private readonly IDeferredMessagesRepository _repository;
        private IRawMessagePublisher _publisher;
        
        public DeferredMessagesManager(IDeferredMessagesRepository repository, TimeSpan delayPrecision, ILog log) :
            base(nameof(DeferredMessagesManager), (int)delayPrecision.TotalMilliseconds, log)
        {
            _repository = repository;
        }

        public void PublishUsing(IRawMessagePublisher publisher)
        {
            _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
        }

        public Task DeferAsync(byte[] message, DateTime deliverAt)
        {
            return _repository.SaveAsync(message, deliverAt);
        }

        public override async Task Execute()
        {
            if (_publisher == null)
            {
                throw new InvalidOperationException("Publisher is not set");
            }

            var messages = await _repository.GetOverdueMessagesAsync(DateTime.UtcNow);
            var removeTasks = new List<Task>();

            try
            {
                foreach (var message in messages)
                {
                    _publisher.Produce(message.Message);
                    removeTasks.Add(_repository.RemoveAsync(message.Key));
                }
            }
            finally
            {
                if (removeTasks.Any())
                {
                    await Task.WhenAll(removeTasks);
                }
            }
        }
    }
}
