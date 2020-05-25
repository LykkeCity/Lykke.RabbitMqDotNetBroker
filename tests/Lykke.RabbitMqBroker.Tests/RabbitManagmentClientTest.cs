// Copyright (c) Lykke Corp.
// Licensed under the MIT License. See the LICENSE file in the project root for more information.

using Lykke.RabbitMqBroker.Tests.Utils;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Lykke.RabbitMqBroker.Tests
{
    [TestFixture, Explicit("Only for manual testing. Doesn't perform correct cleanup")]
    public class RabbitManagmentClientTest
    {
        [Test]
        public void GetDefaultHost()
        {
            var client = new RabbitManagementClient("http://localhost:15672", "guest", "guest");
            var vhosts = client.GetVhosts();
        }

        [Test]
        public void AddCustomHost()
        {
            var client = new RabbitManagementClient("http://localhost:15672", "guest", "guest");
            client.AddVhost("MyHost");
        }

        [Test]
        public void DeleteCustomHost()
        {
            var client = new RabbitManagementClient("http://localhost:15672", "guest", "guest");
            client.DeleteVhost("MyHost");
        }

        [Test]
        public void SetUserPermissions()
        {
            var client = new RabbitManagementClient("http://localhost:15672", "guest", "guest");
            client.SetFullPermissions("MyHost", "guest");
        }
    }

    public sealed class Vhost
    {
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }
    }
}
