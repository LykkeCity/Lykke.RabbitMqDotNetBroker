using System;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace Lykke.RabbitMqBroker.Subscriber.Middleware.Telemetry
{
    public class TelemetryMiddleware<T> : IEventMiddleware<T>
    {
        private const string TelemetryType = "RabbitMq Subscriber";

        private readonly TelemetryClient _telemetry = new TelemetryClient();
        private readonly string _typeName = typeof(T).Name;

        private string _queueName;

        public Task ProcessAsync(IEventContext<T> context)
        {
            if (_queueName == null)
                _queueName = context.Settings.GetQueueOrExchangeName();

            var telemetryOperation = InitTelemetryOperation(_queueName, context.Body.Length);
            try
            {
                return context.InvokeNextAsync();
            }
            catch (Exception e)
            {
                telemetryOperation.Telemetry.Success = false;
                _telemetry.TrackException(e);
                throw;
            }
            finally
            {
                _telemetry.StopOperation(telemetryOperation);
            }
        }

        private IOperationHolder<DependencyTelemetry> InitTelemetryOperation(
            string queueName,
            int binaryLength)
        {
            var operation = _telemetry.StartOperation<DependencyTelemetry>(queueName);
            operation.Telemetry.Type = TelemetryType;
            operation.Telemetry.Target = queueName;
            operation.Telemetry.Name = _typeName;
            operation.Telemetry.Data = $"Binary length {binaryLength}";

            return operation;
        }
    }
}
