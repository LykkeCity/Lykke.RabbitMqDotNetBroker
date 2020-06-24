using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Lykke.RabbitMqBroker.Subscriber.Middleware.ErrorHandling
{
    public class ExceptionSwallowMiddleware<T> : IEventMiddleware<T>
    {
        private readonly ILogger<ExceptionSwallowMiddleware<T>> _logger;

        public ExceptionSwallowMiddleware(ILogger<ExceptionSwallowMiddleware<T>> logger)
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
                _logger.LogError(ex, context.Settings.GetSubscriberName());
            }
            finally
            {
                context.MessageAcceptor.Accept();
            }
        }
    }
}
