using Lykke.RabbitMqBroker.Subscriber;
using NUnit.Framework;

namespace Lykke.RabbitMqBroker.Tests
{
    [TestFixture]
    internal class RabbitMqSubscriptionSettingsTest
    {
        [Test]
        public void PublisherSettingsDefaultNamespaceTest()
        {
            var connString = "test.ConnString";
            var endPoint = "test.Endpoint";

            var shortPubSettings = RabbitMqSubscriptionSettings.ForPublisher(connString, endPoint);

            Assert.AreEqual(connString, shortPubSettings.ConnectionString);
            Assert.Null(shortPubSettings.DeadLetterExchangeName);
            Assert.AreEqual($"{RabbitMqSubscriptionSettings.LykkeNameSpace}.{endPoint}", shortPubSettings.ExchangeName);
            Assert.False(shortPubSettings.IsDurable);
            Assert.Null(shortPubSettings.QueueName);
            Assert.AreEqual(RabbitMqSubscriptionSettings.DefaultReconnectionDelay, shortPubSettings.ReconnectionDelay);
            Assert.AreEqual(RabbitMqSubscriptionSettings.DefaultReconnectionsCountToAlarm, shortPubSettings.ReconnectionsCountToAlarm);
            Assert.AreEqual(string.Empty, shortPubSettings.RoutingKey);
        }

        [Test]
        public void PublisherSettingsCustomNamespaceTest()
        {
            var connString = "test.ConnString";
            var endPoint = "test.Endpoint";
            var namespace1 = "test.Namespace";

            var shortPubSettings = RabbitMqSubscriptionSettings.ForPublisher(
                connString,
                namespace1,
                endPoint);

            Assert.AreEqual(connString, shortPubSettings.ConnectionString);
            Assert.Null(shortPubSettings.DeadLetterExchangeName);
            Assert.AreEqual($"{namespace1}.{endPoint}", shortPubSettings.ExchangeName);
            Assert.False(shortPubSettings.IsDurable);
            Assert.Null(shortPubSettings.QueueName);
            Assert.AreEqual(RabbitMqSubscriptionSettings.DefaultReconnectionDelay, shortPubSettings.ReconnectionDelay);
            Assert.AreEqual(RabbitMqSubscriptionSettings.DefaultReconnectionsCountToAlarm, shortPubSettings.ReconnectionsCountToAlarm);
            Assert.AreEqual(string.Empty, shortPubSettings.RoutingKey);
        }

        [Test]
        public void PublisherSettingsCustomLykkeEndpointTest()
        {
            var endPoint = $"{RabbitMqSubscriptionSettings.LykkeNameSpace}.Endpoint";
            var namespace1 = RabbitMqSubscriptionSettings.LykkeNameSpace;

            var shortPubSettings = RabbitMqSubscriptionSettings.ForPublisher(
                "test.ConnString",
                namespace1,
                endPoint);

            Assert.AreEqual(endPoint, shortPubSettings.ExchangeName);
        }

        [Test]
        public void PublisherSettingsCustomLykkeStartEndpointTest()
        {
            var endPoint = $"{RabbitMqSubscriptionSettings.LykkeNameSpace}.Endpoint";
            var namespace1 = $"{RabbitMqSubscriptionSettings.LykkeNameSpace}.Namespace";

            var shortPubSettings = RabbitMqSubscriptionSettings.ForPublisher(
                "test.ConnString",
                namespace1,
                endPoint);

            Assert.AreEqual($"{namespace1}.{endPoint}", shortPubSettings.ExchangeName);
        }

        [Test]
        public void SubscriberSettingsDefaultNamespacesTest()
        {
            var connString = "test.ConnString";
            var source = "test.Source";
            var endPoint = "test.Endpoint";

            var shortPubSettings = RabbitMqSubscriptionSettings.ForSubscriber(
                connString,
                source,
                endPoint);

            Assert.AreEqual(connString, shortPubSettings.ConnectionString);
            Assert.AreEqual($"{RabbitMqSubscriptionSettings.LykkeNameSpace}.{endPoint}.{source}.dlx", shortPubSettings.DeadLetterExchangeName);
            Assert.AreEqual($"{RabbitMqSubscriptionSettings.LykkeNameSpace}.{source}", shortPubSettings.ExchangeName);
            Assert.False(shortPubSettings.IsDurable);
            Assert.AreEqual($"{RabbitMqSubscriptionSettings.LykkeNameSpace}.{source}.{endPoint}", shortPubSettings.QueueName);
            Assert.AreEqual(RabbitMqSubscriptionSettings.DefaultReconnectionDelay, shortPubSettings.ReconnectionDelay);
            Assert.AreEqual(RabbitMqSubscriptionSettings.DefaultReconnectionsCountToAlarm, shortPubSettings.ReconnectionsCountToAlarm);
            Assert.AreEqual(string.Empty, shortPubSettings.RoutingKey);
        }

        [Test]
        public void SubscriberSettingsCustomNamespacesTest()
        {
            var connString = "test.ConnString";
            var sourceNamespace = "test.SourceNamespace";
            var source = "test.Source";
            var endpointNamespace = "test.EndpointNamespace";
            var endPoint = "test.Endpoint";

            var shortPubSettings = RabbitMqSubscriptionSettings.ForSubscriber(
                connString,
                sourceNamespace,
                source,
                endpointNamespace,
                endPoint);

            Assert.AreEqual(connString, shortPubSettings.ConnectionString);
            Assert.AreEqual($"{endpointNamespace}.{endPoint}.{source}.dlx", shortPubSettings.DeadLetterExchangeName);
            Assert.AreEqual($"{sourceNamespace}.{source}", shortPubSettings.ExchangeName);
            Assert.False(shortPubSettings.IsDurable);
            Assert.AreEqual($"{sourceNamespace}.{source}.{endPoint}", shortPubSettings.QueueName);
            Assert.AreEqual(RabbitMqSubscriptionSettings.DefaultReconnectionDelay, shortPubSettings.ReconnectionDelay);
            Assert.AreEqual(RabbitMqSubscriptionSettings.DefaultReconnectionsCountToAlarm, shortPubSettings.ReconnectionsCountToAlarm);
            Assert.AreEqual(string.Empty, shortPubSettings.RoutingKey);
        }

        [Test]
        public void SubscriberSettingsCustomLykkeNamespacesTest()
        {
            var sourceNamespace = RabbitMqSubscriptionSettings.LykkeNameSpace;
            var source = $"{RabbitMqSubscriptionSettings.LykkeNameSpace}.Source";
            var endpointNamespace = RabbitMqSubscriptionSettings.LykkeNameSpace;
            var endPoint = $"{RabbitMqSubscriptionSettings.LykkeNameSpace}.Endpoint";

            var shortPubSettings = RabbitMqSubscriptionSettings.ForSubscriber(
                "test.ConnString",
                sourceNamespace,
                source,
                endpointNamespace,
                endPoint);

            Assert.AreEqual($"{endPoint}.{source}.dlx", shortPubSettings.DeadLetterExchangeName);
            Assert.AreEqual($"{source}", shortPubSettings.ExchangeName);
            Assert.AreEqual($"{source}.{endPoint}", shortPubSettings.QueueName);
        }

        [Test]
        public void SubscriberSettingsCustomLykkeStartNamespacesTest()
        {
            var sourceNamespace = $"{RabbitMqSubscriptionSettings.LykkeNameSpace}.SourceNamespace";
            var source = $"{RabbitMqSubscriptionSettings.LykkeNameSpace}.Source";
            var endpointNamespace = $"{RabbitMqSubscriptionSettings.LykkeNameSpace}.EndpointNamespace";
            var endPoint = $"{RabbitMqSubscriptionSettings.LykkeNameSpace}.Endpoint";

            var shortPubSettings = RabbitMqSubscriptionSettings.ForSubscriber(
                "test.ConnString",
                sourceNamespace,
                source,
                endpointNamespace,
                endPoint);

            Assert.AreEqual($"{endpointNamespace}.{endPoint}.{source}.dlx", shortPubSettings.DeadLetterExchangeName);
            Assert.AreEqual($"{sourceNamespace}.{source}", shortPubSettings.ExchangeName);
            Assert.AreEqual($"{sourceNamespace}.{source}.{endPoint}", shortPubSettings.QueueName);
        }
    }
}
