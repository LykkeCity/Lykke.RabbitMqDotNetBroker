using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Lykke.RabbitMqBroker.Subscriber.Middleware.ErrorHandling
{
    public class ResilientErrorHandlingMiddleware<T> : IEventMiddleware<T>
    {
        private readonly ILogger<ResilientErrorHandlingMiddleware<T>> _logger;
        private readonly TimeSpan _retryTimeout;
        private readonly int _retryNum;

        public ResilientErrorHandlingMiddleware(
            ILogger<ResilientErrorHandlingMiddleware<T>> logger,
            TimeSpan retryTimeout,
            int retryNum = 5)
        {
            _logger = logger;
            _retryTimeout = retryTimeout;
            _retryNum = retryNum;
        }

        public async Task ProcessAsync(IEventContext<T> context)
        {
            try
            {
                await context.InvokeNextAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Failed to handle the message. Retry in {_retryTimeout.TotalSeconds} sec. Exception {ex}");

                // Retries loop
                for (int i = 0; i < _retryNum; i++)
                {
                    // Adding delay between attempts
                    await Task.Delay(_retryTimeout, context.CancellationToken);

                    try
                    {
                        await context.InvokeNextAsync();
                        return;
                    }
                    catch (Exception ex2)
                    {
                        _logger.LogWarning(
                            $"Retry attempt: failed to handle the message for the {i + 1} time. Retry in {_retryTimeout.TotalSeconds} sec. Exception {ex2}");
                    }
                }

                throw;
            }
        }
    }
}
