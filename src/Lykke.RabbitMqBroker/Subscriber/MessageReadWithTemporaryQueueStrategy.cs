﻿using System;
using System.Collections.Generic;
using RabbitMQ.Client;

namespace Lykke.RabbitMqBroker.Subscriber
{
    public class MessageReadWithTemporaryQueueStrategy : IMessageReadStrategy
    {
        private readonly string _routingKey;

        public MessageReadWithTemporaryQueueStrategy(string routingKey = "")
        {
            _routingKey = routingKey;
        }

        public string Configure(RabbitMqSubscriptionSettings settings, IModel channel)
        {
            // If specified queue name is empty generate random name
            var queueName = String.IsNullOrEmpty(settings.QueueName)
                ? settings.ExchangeName + "." + Guid.NewGuid().ToString()
                : settings.QueueName;

            // For random name set autodelete
            //var autodelete = String.IsNullOrEmpty(settings.QueueName) ? true : false;

            // autodelete is always reverse from isdurable
            var autodelete = !settings.IsDurable;
            IDictionary<string, object> args = null;
            if (!string.IsNullOrEmpty(settings.DeadLetterExchangeName))
            {
                var poisonQueueName = $"{queueName}-poison";
                args = new Dictionary<string, object>
                {
                    { "x-dead-letter-exchange", settings.DeadLetterExchangeName }
                };
                channel.ExchangeDeclare(settings.DeadLetterExchangeName, "direct", durable: true);
                channel.QueueDeclare(poisonQueueName, durable: settings.IsDurable, exclusive: false, autoDelete: false);
                channel.QueueBind(poisonQueueName, settings.DeadLetterExchangeName, settings.RoutingKey);

                var deadLetterQueueName = settings.DeadLetterExchangeName;

                channel.QueueDeclare(queue: deadLetterQueueName, durable: true, exclusive: false, autoDelete: false);
                channel.QueueBind(queue: deadLetterQueueName, exchange: settings.DeadLetterExchangeName, routingKey: string.Empty);
            }

            settings.QueueName = channel.QueueDeclare(queueName, durable: settings.IsDurable, exclusive: false, autoDelete: autodelete, arguments: args).QueueName;


            channel.QueueBind(
                queue: settings.QueueName,
                exchange: settings.ExchangeName,
                routingKey: _routingKey);

            return settings.QueueName;
        }

    }

}
