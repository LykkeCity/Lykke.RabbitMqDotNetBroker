using System;
using Common.Log;
using Lykke.RabbitMqBroker.Subscriber;

namespace Lykke.RabbitMqBroker
{
    public sealed class DeadQueueErrorHandlingStrategy : IErrorHandlingStrategy
    {
        private readonly ILog _log;
        private readonly RabbitMqSubscribtionSettings _settings;

        public DeadQueueErrorHandlingStrategy(ILog log, RabbitMqSubscribtionSettings settings)
        {
            _log = log;
            _settings = settings;
        }
        public void Execute(Action handler, IMessageAcceptor ma)
        {
            try
            {
                handler();  
                ma.Accept();
            }
            catch (Exception ex)
            {
                _log.WriteWarningAsync(nameof(ResilientErrorHandlingStrategy), _settings.GetSubscriberName(), "Message handling", $"Failed to handle the message. Send it to poison queue {_settings.QueueName + "-poison"}. Exception {ex}");
                ma.Reject();
            }
        }
    }
}