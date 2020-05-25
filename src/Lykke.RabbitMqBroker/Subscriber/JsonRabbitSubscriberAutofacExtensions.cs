using System;
using Autofac;
using JetBrains.Annotations;

namespace Lykke.RabbitMqBroker.Subscriber
{
    /// <summary>
    /// Extension for JsonRabbitSubscriber based class registration in Autofac container.
    /// </summary>
    [PublicAPI]
    public static class JsonRabbitSubscriberAutofacExtensions
    {
        /// <summary>
        /// Registers type inherited from <see cref="JsonRabbitSubscriber{TMessage}"/> in Autofac container.
        /// </summary>
        /// <typeparam name="TSubscriber">Class type.</typeparam>
        /// <typeparam name="TMessage">Message type.</typeparam>
        /// <param name="builder">Autofac container builder.</param>
        /// <param name="rabbitMqConnString">Connection string to RabbitMq.</param>
        /// <param name="exchangeName">Exchange name.</param>
        /// <param name="queueName">Queu name.</param>
        public static void RegisterJsonRabbitSubscriber<TSubscriber, TMessage>(
            [NotNull] this ContainerBuilder builder,
            [NotNull] string rabbitMqConnString,
            [NotNull] string exchangeName,
            [NotNull] string queueName
        ) where TSubscriber : JsonRabbitSubscriber<TMessage>
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            if (string.IsNullOrWhiteSpace(rabbitMqConnString))
                throw new ArgumentNullException(nameof(rabbitMqConnString));

            if (string.IsNullOrWhiteSpace(exchangeName))
                throw new ArgumentNullException(nameof(exchangeName));

            if (string.IsNullOrWhiteSpace(queueName))
                throw new ArgumentNullException(nameof(queueName));

            var settings = RabbitMqSubscriptionSettings.ForSubscriber(
                rabbitMqConnString,
                exchangeName,
                queueName);

            builder.RegisterType<TSubscriber>()
                .As<IStartStop>()
                .WithParameter(TypedParameter.From(settings))
                .SingleInstance();
        }
    }
}
