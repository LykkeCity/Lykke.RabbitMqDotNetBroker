using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Lykke.RabbitMqBroker.Logging
{
    public class OutgoingMessageLogger
    {
        private readonly List<string> _filteredMessageTypes;
        private readonly ILogger _logger;

        public OutgoingMessageLogger(ILogger logger)
        {
            _logger = logger;

            var ignoredTypesStr = Environment.GetEnvironmentVariable("FILTERED_MESSAGE_TYPES");
            _filteredMessageTypes = ignoredTypesStr?.Split(',').ToList()
                                   ?? new List<string>();
        }

        /// <summary>
        /// </summary>
        /// <param name="filteredMessageTypes">Types of outgoing messages that should not be logged</param>
        /// <param name="logger"></param>
        /// <exception cref="NullReferenceException"></exception>
        public OutgoingMessageLogger(List<string> filteredMessageTypes, ILogger logger)
        {
            _filteredMessageTypes = filteredMessageTypes ?? throw new NullReferenceException(nameof(filteredMessageTypes));
            _logger = logger;
        }

        public void Log(OutgoingMessage message)
        {
            var typeName = message.MessageTypeName;
            if(_filteredMessageTypes.Contains(typeName)) return;

            _logger.LogInformation(message.ToString());
        }
    }
}
