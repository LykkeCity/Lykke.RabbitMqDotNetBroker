using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace Lykke.RabbitMqBroker.Logging
{
    public class OutgoingMessage
    {
        public string MessageTypeFullName { get; set; }
        
        public string MessageTypeName { get; set; }
        
        public SerializationFormat Format { get; set; }

        public string Message { get; set; }

        public Dictionary<string, object> Headers { get; set; }

        public string Exchange { get; set; }

        public string RoutingKey { get; set; }

        public DateTime Timestamp { get; set; }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine("(||");
            sb.AppendLine(JsonConvert.SerializeObject(this, Formatting.Indented));
            sb.AppendLine("||)");

            return sb.ToString();
        }
    }
}
