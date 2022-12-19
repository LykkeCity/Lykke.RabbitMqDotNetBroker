using System;
using System.Collections.Generic;
using System.Text;
using Lykke.RabbitMqBroker.Publisher;
using Lykke.RabbitMqBroker.Publisher.Strategies;
using Moq;
using NUnit.Framework;
using RabbitMQ.Client;
using RabbitMQ.Client.Framing;

namespace Lykke.RabbitMqBroker.Tests.PublisherStrategies
{
    public class BasicPublishStrategyTests
    {
        [TestCaseSource(nameof(TestCases))]
        public void Ctor_SettingsAreNotNull_ShouldNotThrow(
            Func<RabbitMqSubscriptionSettings, IRabbitMqPublishStrategy> ctor)
        {
            var settings = RabbitMqSubscriptionSettings.ForPublisher("connectionString", "endpoint");
            Assert.DoesNotThrow(() => ctor(settings));
        }

        [TestCaseSource(nameof(TestCases))]
        public void Ctor_SettingsAreNull_ShouldThrow(Func<RabbitMqSubscriptionSettings, IRabbitMqPublishStrategy> ctor)
        {
            RabbitMqSubscriptionSettings settings = null;
            Assert.Catch<ArgumentNullException>(() => ctor(settings));
        }

        [TestCaseSource(nameof(TestCasesWithExchangeType))]
        public void Configure_ExchangeTypesAreValid(Func<RabbitMqSubscriptionSettings, IRabbitMqPublishStrategy> ctor,
            string exchangeType)
        {
            var settings = RabbitMqSubscriptionSettings.ForPublisher("connectionString", "endpoint");
            var strategy = ctor(settings);
            var channel = new Mock<IModel>();

            strategy.Configure(channel.Object);

            channel.Verify(model => model.ExchangeDeclare(It.IsAny<string>(), 
                    exchangeType, // exchange type is checked
                    It.IsAny<bool>(), 
                    It.IsAny<bool>(), 
                    null), 
                Times.Once());
        }
        
        [TestCaseSource(nameof(TestCasesWithRoutingKey))]
        public void Publish_HeadersArePassed(Func<RabbitMqSubscriptionSettings, IRabbitMqPublishStrategy> ctor, 
            string routingKey)
        {
            var settings = RabbitMqSubscriptionSettings.ForPublisher("connectionString", "endpoint");
            var strategy = ctor(settings);
            var channel = new Mock<IModel>();
            channel.Setup(x => x.CreateBasicProperties()).Returns(new BasicProperties());

            var header = "hello-header";
            var headerValue = "hello-world";
            var headers = new Dictionary<string, object>()
            {
                {header, headerValue}
            };

            var body = Encoding.UTF8.GetBytes("body");

            var message = new RawMessage(body, routingKey, headers);

            strategy.Publish(channel.Object, message);
            
            // extension methods cannot be verified with moq, so we check the underlying method 
            channel.Verify(model => model.BasicPublish(It.IsAny<string>(), 
                    routingKey,
                    false,
                    It.Is<IBasicProperties>(props => 
                        props.Headers.ContainsKey(header) 
                        && props.Headers[header].ToString() == headerValue),
                    body), 
                Times.Once());
        }

        #region TestCases

        private static object[] TestCases =
        {
            new object[] {(RabbitMqSubscriptionSettings settings) => new DirectPublishStrategy(settings),},
            new object[] {(RabbitMqSubscriptionSettings settings) => new FanoutPublishStrategy(settings),},
            new object[] {(RabbitMqSubscriptionSettings settings) => new TopicPublishStrategy(settings),}
        };

        private static object[] TestCasesWithExchangeType =
        {
            new object[]
            {
                (RabbitMqSubscriptionSettings settings) => new DirectPublishStrategy(settings), "direct",
            },
            new object[]
            {
                (RabbitMqSubscriptionSettings settings) => new FanoutPublishStrategy(settings), "fanout",
            },
            new object[]
            {
                (RabbitMqSubscriptionSettings settings) => new TopicPublishStrategy(settings), "topic",
            }
        };
        
        private static object[] TestCasesWithRoutingKey =
        {
            new object[]
            {
                (RabbitMqSubscriptionSettings settings) => new DirectPublishStrategy(settings), "direct-route",
            },
            new object[]
            {
                (RabbitMqSubscriptionSettings settings) => new FanoutPublishStrategy(settings), "",
            },
            new object[]
            {
                (RabbitMqSubscriptionSettings settings) => new TopicPublishStrategy(settings), "topic-route",
            }
        };

        #endregion
    }
}
