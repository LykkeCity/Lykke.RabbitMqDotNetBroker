using System;
using Autofac;
using Autofac.Builder;
using JetBrains.Annotations;

namespace Lykke.RabbitMqBroker.Publisher
{
    /// <summary>
    /// Extension for JsonRabbitPublisher registration in Autofac container.
    /// </summary>
    [PublicAPI]
    public static class JsonRabbitPublisherAutofacExtensions
    {
        /// <summary>
        /// Registers <see cref="JsonRabbitPublisher{TMessage}"/> in Autofac container.
        /// </summary>
        /// <typeparam name="TMessage">Message type.</typeparam>
        /// <param name="builder">Autofac container builder.</param>
        /// <param name="rabbitMqConnString">Connection string to RabbitMq.</param>
        /// <param name="exchangeName">Exchange name.</param>
        public static IRegistrationBuilder<JsonRabbitPublisher<TMessage>, ConcreteReflectionActivatorData, SingleRegistrationStyle> RegisterJsonRabbitPublisher<TMessage>(
            [NotNull] this ContainerBuilder builder,
            [NotNull] string rabbitMqConnString,
            [NotNull] string exchangeName
        )
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            if (string.IsNullOrWhiteSpace(rabbitMqConnString))
                throw new ArgumentNullException(nameof(rabbitMqConnString));

            if (string.IsNullOrWhiteSpace(exchangeName))
                throw new ArgumentNullException(nameof(exchangeName));

            return builder.RegisterType<JsonRabbitPublisher<TMessage>>()
                .As<IRabbitPublisher<TMessage>>()
                .As<IStartable>()
                .WithParameter("connectionString", rabbitMqConnString)
                .WithParameter("exchangeName", exchangeName)
                .SingleInstance();
        }
    }
}
