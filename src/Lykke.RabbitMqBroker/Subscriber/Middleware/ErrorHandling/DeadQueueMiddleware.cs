using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Lykke.RabbitMqBroker.Subscriber.Middleware.ErrorHandling
{
    public class DeadQueueMiddleware<T> : IEventMiddleware<T>
    {
        private readonly ILogger<DeadQueueMiddleware<T>> _logger;

        public DeadQueueMiddleware(ILogger<DeadQueueMiddleware<T>> logger)
        {
            _logger = logger;
        }

        public async Task ProcessAsync(IEventContext<T> context)
        {
            try
            {
                await context.InvokeNextAsync();
                context.MessageAcceptor.Accept();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    $"Failed to handle the message. Send it to poison queue {context.Settings.GetSubscriberName()} {context.Settings.QueueName}-poison. Exception {ex}");
                context.MessageAcceptor.Reject();
            }
        }
    }
}
