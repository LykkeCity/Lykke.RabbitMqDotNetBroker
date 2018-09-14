// Copyright (c) Lykke Corp.
// Licensed under the MIT License. See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Common.Log;

namespace Lykke.RabbitMqBroker.Publisher.DeferredMessages
{
    internal class DeferredMessagesManager : TimerPeriod
    {
        private readonly IDeferredMessagesRepository _repository;
        private IRawMessagePublisher _publisher;
        
        [Obsolete]
        public DeferredMessagesManager(IDeferredMessagesRepository repository, TimeSpan delayPrecision, ILog log) :
            base(nameof(DeferredMessagesManager), (int)delayPrecision.TotalMilliseconds, log)
        {
            _repository = repository;
        }

        public DeferredMessagesManager(
            [NotNull] IDeferredMessagesRepository repository, 
            TimeSpan delayPrecision,
            [NotNull] ILogFactory logFactory) :

            base(delayPrecision, logFactory)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public void PublishUsing(IRawMessagePublisher publisher)
        {
            _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
        }

        public Task DeferAsync(RawMessage message, DateTime deliverAt)
        {
            return _repository.SaveAsync(message, deliverAt);
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
