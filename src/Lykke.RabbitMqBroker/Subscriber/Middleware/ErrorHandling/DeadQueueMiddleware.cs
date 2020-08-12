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
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    $"Failed to handle the message. Sending it to poison queue {context.Settings.GetSubscriberName()} {context.Settings.QueueName}-poison.");
                context.MessageAcceptor.Reject();
            }
        }
    }
}
