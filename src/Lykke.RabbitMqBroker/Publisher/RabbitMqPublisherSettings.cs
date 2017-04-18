using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lykke.RabbitMqBroker.Publisher
{
    public class RabbitMqPublisherSettings
    {
        public string ConnectionString { get; set; }
        public string ExchangeName { get; set; }
    }

}
