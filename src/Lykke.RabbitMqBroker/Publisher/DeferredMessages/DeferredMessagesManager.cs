// Copyright (c) Lykke Corp.
// Licensed under the MIT License. See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Lykke.RabbitMqBroker.Publisher.DeferredMessages
{
    internal class DeferredMessagesManager : IStartStop
    {
        private readonly IDeferredMessagesRepository _repository;
        private readonly Timer _timer;
        private readonly TimeSpan _period;
        private readonly ILogger<DeferredMessagesManager> _logger;

        private IRawMessagePublisher _publisher;

        public DeferredMessagesManager(
            IDeferredMessagesRepository repository,
            TimeSpan delayPrecision,
            ILogger<DeferredMessagesManager> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _period = delayPrecision;
            _timer = new Timer(Execute, null, TimeSpan.FromMilliseconds(-1), delayPrecision);
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void PublishUsing(IRawMessagePublisher publisher)
        {
            _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
        }

        public Task DeferAsync(RawMessage message, DateTime deliverAt)
        {
            return _repository.SaveAsync(message, deliverAt);
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
            ProcessMessagesAsync().GetAwaiter().GetResult();
        }

        private async Task ProcessMessagesAsync()
        {
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
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to publish messages");
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
